using System;
using System.Collections.Generic;
using System.IO;

namespace Tagbag.Core.Input;

public enum TokenType
{
    Symbol,
    String,
    Number,
    ParenOpen,
    ParenClose,
}

public record Token(int Pos, TokenType Type, string Text)
{
    public bool Matches(Token? other)
    {
        if (other is Token token)
            return Type == token.Type && Text == other.Text;
        return false;
    }
}

public class Tokenizer
{
    public static LinkedList<Token> GetTokens(string input)
    {
        var tokenizer = new Tokenizer(input);
        tokenizer.Run();
        return tokenizer.result;
    }

    // input
    private string input;
    private StringReader reader;
    private int peeked;
    private int charCount = 0;

    // internal
    private char[] accumulator = new char[1024];
    private int accumulatorIndex = 0;
    private int discarded = 0;

    // output
    private LinkedList<Token> result = new LinkedList<Token>();

    private Tokenizer(string input)
    {
        this.input = input;
        reader = new StringReader(input);
    }

    private void Run()
    {
        while (peeked != -1)
        {
            peeked = reader.Peek();
            switch (peeked)
            {
                case '(':
                    Read();
                    AddToken(TokenType.ParenOpen);
                    break;

                case ')':
                    Read();
                    AddToken(TokenType.ParenClose);
                    break;

                case '|':
                    Read();
                    AddToken(TokenType.Symbol);
                    break;

                case '"':
                    ProcessString();
                    break;

                default:
                    if (Char.IsWhiteSpace((char)peeked))
                        ProcessWhiteSpace();
                    else if (Char.IsAsciiDigit((char)peeked))
                        ProcessNumber();
                    else
                        ProcessSymbol();
                    break;
            }
        }
    }

    private void Read()
    {
        accumulator[accumulatorIndex] = (char)reader.Read();
        accumulatorIndex++;
        charCount++;
        peeked = reader.Peek();
    }

    private void Discard()
    {
        reader.Read();
        charCount++;
        discarded++;
        peeked = reader.Peek();
    }

    private void AddToken(TokenType type)
    {
        var token = new Token(
            charCount - accumulatorIndex - discarded,
            type,
            new String(accumulator, 0, accumulatorIndex));
        result.AddLast(token);
        accumulatorIndex = 0;
        discarded = 0;
    }

    private void Throw(string msg)
    {
        throw new TokenizerException(input, charCount, msg);
    }

    private bool IsDelim(int c)
    {
        return Char.IsWhiteSpace((char)c)
            || c == '(' || c == ')' || c == '"' || c == '|'
            || c == -1;
    }

    private void ProcessString()
    {
        Discard();
        var prev = 0;
        while (true)
        {
            peeked = reader.Peek();
            switch (peeked)
            {
                case -1:
                    Throw("Unexpected end of stream");
                    break;

                case '"':
                    Discard();
                    AddToken(TokenType.String);
                    return;

                default:
                    Read();
                    break;
            }
            prev = peeked;
        }
    }

    private void ProcessWhiteSpace()
    {
        while (Char.IsWhiteSpace((char)peeked))
        {
            Discard();
            peeked = reader.Peek();
        }
    }

    private void ProcessNumber()
    {
        while (Char.IsAsciiDigit((char)peeked))
        {
            Read();
            peeked = reader.Peek();
        }

        if (!IsDelim(peeked))
            Throw("Expected delimiter after number");

        AddToken(TokenType.Number);
    }

    private void ProcessSymbol()
    {
        while (!IsDelim(peeked))
        {
            Read();
        }
        AddToken(TokenType.Symbol);
    }
}

public class TokenizerException : ArgumentException
{
    public string Input;
    public int Position;
    new public string Message;

    public TokenizerException(string input, int position, string msg) : base(msg)
    {
        Input = input;
        Position = position;
        Message = msg;
    }
}
