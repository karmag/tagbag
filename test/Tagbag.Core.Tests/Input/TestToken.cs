using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Tagbag.Core.Input;

namespace Tagbag.Core.Test.Input;

[TestClass]
public class TestToken
{
    [TestMethod]
    public void TestBasicTokenization()
    {
        Assert.AreEqual(GetFirst("symbol"), new Token(0, TokenType.Symbol, "symbol"));
        Assert.AreEqual(GetFirst("\"str\""), new Token(0, TokenType.String, "str"));
        Assert.AreEqual(GetFirst("123"), new Token(0, TokenType.Number, "123"));
        Assert.AreEqual(GetFirst("("), new Token(0, TokenType.ParenOpen, "("));
        Assert.AreEqual(GetFirst(")"), new Token(0, TokenType.ParenClose, ")"));
        Assert.AreEqual(GetFirst("|"), new Token(0, TokenType.Symbol, "|"));

        Assert.AreEqual(GetFirst("abc123"), new Token(0, TokenType.Symbol, "abc123"));
    }

    [TestMethod]
    public void TestTokenization()
    {
        CollectionAssert.AreEqual(GetTexts("a and b"),
                                  new string[]{ "a", "and", "b" });

        CollectionAssert.AreEqual(GetTexts("(a|b)"),
                                  new string[]{ "(", "a", "|", "b", ")" });

        CollectionAssert.AreEqual(GetTexts("123\"abc\""),
                                  new string[]{ "123", "abc" });

        CollectionAssert.AreEqual(GetTexts("a<=b"),
                                  new string[]{ "a", "<=", "b" });

        CollectionAssert.AreEqual(GetTexts("a~=b"),
                                  new string[]{ "a", "~=", "b" });

        CollectionAssert.AreEqual(GetTexts("a>=<10"),
                                  new string[]{ "a", ">=<", "10" });
    }

    private Token GetFirst(string input)
    {
        var result = Tokenizer.GetTokens(input);
        Assert.IsNotNull(result);
        Assert.HasCount(1, result);
        if (result?.First?.Value is Token token)
            return token;
        throw new ArgumentException("error");
    }

    private string[] GetTexts(string input)
    {
        var result = Tokenizer.GetTokens(input);
        var arr = new string[result.Count];
        int i = 0;
        foreach (var token in result)
        {
            arr[i] = token.Text;
            i++;
        }
        return arr;
    }
}
