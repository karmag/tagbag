using System;
using System.Collections.Generic;

namespace Tagbag.Core.Input;

public class BuildException(string msg) : ArgumentException(msg)
{
    private Token? _Token;

    public BuildException With(Token? token)
    {
        _Token = token;
        return this;
    }

    public string FullMessage()
    {
        if (_Token is Token token)
            return $"{Message} (\"{token.Text}\":{token.Pos})";
        return Message;
    }
}

public class TagBuilder
{
    public static ITagOperation Build(string input)
    {
        return Build(Tokenizer.GetTokens(input));
    }

    public static ITagOperation Build(LinkedList<Token> tokens)
    {
        var parts = Split(tokens);
        var operations = new LinkedList<ITagOperation>();

        foreach (var partTokens in parts)
            operations.AddLast(ParseOperation(partTokens));

        if (operations.Count == 1)
        {
            if (operations?.First?.Value is ITagOperation op)
                return op;
        }
        else if (operations.Count > 1)
        {
            return TagOperation.Combine(operations);
        }

        throw new ArgumentException("No legal combination of tag operations");
    }

    private static LinkedList<LinkedList<Token>> Split(LinkedList<Token> tokens)
    {
        var pipe = new Token(0, TokenType.Symbol, "|");
        var parts = new LinkedList<LinkedList<Token>>();
        var temp = new LinkedList<Token>();

        foreach (var tkn in tokens)
        {
            if (tkn.Matches(pipe))
            {
                if (temp.Count > 0)
                {
                    parts.AddLast(temp);
                    temp = new LinkedList<Token>();
                }
            }
            else
            {
                temp.AddLast(tkn);
            }
        }

        if (temp.Count > 0)
            parts.AddLast(temp);

        return parts;
    }

    private static ITagOperation ParseOperation(LinkedList<Token> tokens)
    {
        var result = new LinkedList<ITagOperation>();
        Token? inTag = null;
        Token? inOp = null;
        LinkedListNode<Token>? values = null;

        if (tokens.Count >= 1)
            inTag = tokens.First?.Value;

        if (tokens.Count == 2)
            values = tokens.First?.Next;

        if (tokens.Count == 3)
        {
            inOp = tokens.First?.Next?.Value;
            values = tokens.First?.Next?.Next;
        }

        if (inTag is Token tagToken)
        {
            if (tagToken.Type != TokenType.Symbol)
                throw new BuildException("Tag must be symbol").With(tagToken);

            string tag = tagToken.Text;
            string operation = "";

            if (tag.StartsWith("+") || tag.StartsWith("-"))
            {
                operation = tag.Substring(0, 1);
                tag = tag.Substring(1);
            }

            if (inOp is Token op)
            {
                if (op.Type != TokenType.Symbol)
                    throw new BuildException("Operation must be a symbol").With(op);

                if (operation.Length > 0)
                    throw new BuildException("Multiple operations given").With(tagToken);

                operation = op.Text;
            }

            if (operation.Length == 0)
                operation = "+";

            switch (operation)
            {
                case "+":
                case "-":
                case "=":
                    break;
                default:
                    throw new BuildException($"Unknown operation '{operation}'");
            }

            if (values == null)
            {
                switch (operation)
                {
                    case "+":
                        result.AddLast(TagOperation.Add(tag));
                        break;
                    case "-":
                        result.AddLast(TagOperation.Remove(tag));
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown operation '{operation}'");
                }
            }
                               
            while (values != null)
            {
                if (values?.Value is Token val)
                {
                    switch (val.Type)
                    {
                        case TokenType.Symbol:
                        case TokenType.String:
                            switch (operation)
                            {
                                case "+":
                                    result.AddLast(TagOperation.Add(tag, val.Text));
                                    break;
                                case "-":
                                    result.AddLast(TagOperation.Remove(tag, val.Text));
                                    break;
                                case "=":
                                    result.AddLast(TagOperation.Set(tag, val.Text));
                                    break;
                                default:
                                    throw new InvalidOperationException($"Unknown operation '{operation}'");
                            }
                            break;

                        case TokenType.Number:
                            var i = int.Parse(val.Text);
                            switch (operation)
                            {
                                case "+":
                                    result.AddLast(TagOperation.Add(tag, i));
                                    break;
                                case "-":
                                    result.AddLast(TagOperation.Remove(tag, i));
                                    break;
                                case "=":
                                    result.AddLast(TagOperation.Set(tag, i));
                                    break;
                                default:
                                    throw new InvalidOperationException($"Unknown operation '{operation}'");
                            }
                            break;

                        default:
                            throw new BuildException("Bad value type").With(val);
                    }
                }

                values = values?.Next;
            }
        }

        if (result.Count == 0)
            throw new InvalidOperationException("Empty result set");

        if (result.Count == 1 && result.First?.Value is ITagOperation singleOp)
            return singleOp;

        return TagOperation.Combine(result);
    }
}

public class FilterBuilder
{
    public static IFilter Build(string input)
    {
        return Build(Tokenizer.GetTokens(input));
    }

    public static IFilter Build(LinkedList<Token> tokens)
    {
        if (tokens.Count > 3)
            throw new BuildException("Too many tokens");

        if (tokens.First?.Value is Token a)
        {
            if (tokens.First?.Next?.Value is Token b)
            {
                if (tokens.First?.Next?.Next?.Value is Token c)
                    return Build3(a, b, c);

                return Build2(a, b);
            }

            return Build1(a);
        }

        throw new InvalidOperationException("No tokens");
    }

    private static IFilter Build1(Token a)
    {
        if (a.Type == TokenType.Symbol)
            return Filter.Has(a.Text);
        throw new BuildException("Tag must be a symbol").With(a);
    }

    private static IFilter Build2(Token a, Token b)
    {
        if (a.Type == TokenType.Symbol)
        {
            switch (b.Type)
            {
                case TokenType.String:
                case TokenType.Symbol:
                    return Filter.Has(a.Text, b.Text);

                case TokenType.Number:
                    return Filter.Has(a.Text, int.Parse(b.Text));

                default:
                    throw new BuildException("Unknown value type").With(b);
            }
        }

        throw new BuildException("Tag must be a symbol").With(a);
    }

    private static IFilter Build3(Token a, Token b, Token c)
    {
        if (a.Type != TokenType.Symbol)
            throw new BuildException("Tag must be a symbol").With(a);

        if (b.Type != TokenType.Symbol)
            throw new BuildException("Operator must be a symbol").With(b);

        switch (b.Text)
        {
            case "=":
                switch (c.Type)
                {
                    case TokenType.String:
                    case TokenType.Symbol:
                        return Filter.Has(a.Text, c.Text);

                    case TokenType.Number:
                        return Filter.Has(a.Text, int.Parse(c.Text));
                }
                break;

            case "~=":
                switch (c.Type)
                {
                    case TokenType.String:
                    case TokenType.Symbol:
                        return Filter.Regex(a.Text, c.Text);

                    default:
                        throw new BuildException("Regex operator requires string value").With(c);
                }

            case "<":
            case ">":
            case "<=":
            case ">=":
                switch (c.Type)
                {
                    case TokenType.Number:
                        return Filter.Math(a.Text, b.Text, int.Parse(c.Text));

                    default:
                        throw new BuildException("Math operator requires int value").With(c);
                }

            default:
                throw new BuildException("Unknown operator").With(b);
        }

        throw new BuildException("Unknown value type").With(c);
    }

    /*
      a
      not a
      a and b
      a or not b and c
      tag value     :: tag = value
      a = 10        :: tag has value
      a = hello     :: -"-
      a ~= "regexp" :: regexp found in string value
      a = (b c d)   :: at least one of the values match
      a == (b c d)  :: all of the values match

      a == (b c) and not tag value or q
    */
}
