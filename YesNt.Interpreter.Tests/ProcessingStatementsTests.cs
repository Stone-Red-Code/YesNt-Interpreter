using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace YesNt.Interpreter.Tests;

[TestClass]
public class ProcessingStatementsTests
{
    [TestMethod]
    public void MultiplicationTest()
    {
        YesNtAssert.IsLineEqual("10 * 10 calc", "100");
    }

    [TestMethod]
    public void DivisionTest()
    {
        YesNtAssert.IsLineEqual("90 / 4 calc", "22.5");
    }

    [TestMethod]
    public void AdditionTest()
    {
        YesNtAssert.IsLineEqual("10 + 10 calc", "20");
    }

    [TestMethod]
    public void SubtractionTest()
    {
        YesNtAssert.IsLineEqual("10 - 10 calc", "0");
    }

    [TestMethod]
    public void ModulusTest()
    {
        YesNtAssert.IsLineEqual("10 % 3 calc", "1");
    }

    [TestMethod]
    public void ExponentiationTest()
    {
        YesNtAssert.IsLineEqual("2 ^ 3 calc", "8");
    }
}
