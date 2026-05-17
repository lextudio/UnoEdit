#nullable enable

using System.Collections;
using System.Globalization;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
#if WINDOWS_APP_SDK
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using Microsoft.UI.Dispatching;
#endif
using MSTestAssert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace NUnit.Framework;

[AttributeUsage(AttributeTargets.Class)]
public sealed class TestFixtureAttribute : TestClassAttribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class TestAttribute : TestMethodAttribute
{
    public override TestResult[] Execute(ITestMethod testMethod)
    {
#if WINDOWS_APP_SDK
        var dispatcherQueue = UITestMethodAttribute.DispatcherQueue;
        if (dispatcherQueue is not null && !dispatcherQueue.HasThreadAccess)
            return ExecuteOnDispatcher(dispatcherQueue, testMethod);
#endif

        return ExecuteCore(testMethod);
    }

    private static TestResult[] ExecuteCore(ITestMethod testMethod)
    {
        var started = DateTimeOffset.UtcNow;
        object? instance = null;
        try
        {
            var type = testMethod.MethodInfo.DeclaringType!;
            instance = testMethod.MethodInfo.IsStatic ? null : Activator.CreateInstance(type);

            InvokeMarkedMethods(type, instance, typeof(OneTimeSetUpAttribute));
            InvokeMarkedMethods(type, instance, typeof(SetUpAttribute));

            var arguments = CoerceArguments(testMethod.Arguments, testMethod.MethodInfo.GetParameters());
            var result = testMethod.MethodInfo.Invoke(instance, arguments);
            if (result is Task task)
                task.GetAwaiter().GetResult();

            return [new TestResult
            {
                Outcome = UnitTestOutcome.Passed,
                Duration = DateTimeOffset.UtcNow - started
            }];
        }
        catch (Exception ex)
        {
            var failure = ex is System.Reflection.TargetInvocationException { InnerException: { } inner }
                ? inner
                : ex;

            return [new TestResult
            {
                Outcome = failure is AssertInconclusiveException ? UnitTestOutcome.Inconclusive : UnitTestOutcome.Failed,
                TestFailureException = failure,
                Duration = DateTimeOffset.UtcNow - started
            }];
        }
        finally
        {
            if (instance is not null)
                InvokeMarkedMethods(testMethod.MethodInfo.DeclaringType!, instance, typeof(TearDownAttribute));
        }
    }

#if WINDOWS_APP_SDK
    private static TestResult[] ExecuteOnDispatcher(DispatcherQueue dispatcherQueue, ITestMethod testMethod)
    {
        TestResult[]? results = null;
        Exception? exception = null;
        using var completed = new ManualResetEventSlim();

        if (!dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                results = ExecuteCore(testMethod);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                completed.Set();
            }
        }))
        {
            return [new TestResult
            {
                Outcome = UnitTestOutcome.Failed,
                TestFailureException = new InvalidOperationException("Failed to enqueue test on the WinUI dispatcher.")
            }];
        }

        completed.Wait();
        if (exception is not null)
            throw exception;

        return results ?? [new TestResult
        {
            Outcome = UnitTestOutcome.Failed,
            TestFailureException = new InvalidOperationException("WinUI dispatcher completed without a test result.")
        }];
    }
#endif

    private static void InvokeMarkedMethods(Type type, object? instance, Type attributeType)
    {
        foreach (var method in type.GetMethods(System.Reflection.BindingFlags.Instance
                                               | System.Reflection.BindingFlags.Static
                                               | System.Reflection.BindingFlags.Public
                                               | System.Reflection.BindingFlags.NonPublic))
        {
            if (!method.GetCustomAttributes(attributeType, inherit: true).Any())
                continue;

            method.Invoke(method.IsStatic ? null : instance, null);
        }
    }

    private static object?[] CoerceArguments(object?[]? arguments, ParameterInfo[] parameters)
    {
        arguments ??= [];
        if (arguments.Length != parameters.Length)
            return arguments;

        var coerced = new object?[arguments.Length];
        for (var i = 0; i < arguments.Length; i++)
        {
            var argument = arguments[i];
            var parameterType = Nullable.GetUnderlyingType(parameters[i].ParameterType) ?? parameters[i].ParameterType;

            if (argument is null || parameterType.IsInstanceOfType(argument))
            {
                coerced[i] = argument;
                continue;
            }

            if (parameterType.IsEnum)
            {
                coerced[i] = Enum.ToObject(parameterType, argument);
                continue;
            }

            coerced[i] = argument is IConvertible && typeof(IConvertible).IsAssignableFrom(parameterType)
                ? Convert.ChangeType(argument, parameterType, CultureInfo.InvariantCulture)
                : argument;
        }

        return coerced;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class SetUpAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class OneTimeSetUpAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class TearDownAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class TestCaseAttribute : Attribute, ITestDataSource
{
    private readonly object?[] _data;

    public TestCaseAttribute(params object?[]? data)
    {
        _data = data ?? [null];
    }

    public IEnumerable<object?[]> GetData(System.Reflection.MethodInfo methodInfo)
    {
        yield return _data;
    }

    public string? GetDisplayName(System.Reflection.MethodInfo methodInfo, object?[]? data)
        => data is null ? null : $"{methodInfo.Name}({string.Join(", ", data.Select(ValueToString))})";

    private static string ValueToString(object? value) => value switch
    {
        null => "null",
        string s => "\"" + s + "\"",
        _ => value.ToString() ?? string.Empty
    };
}

public static class Assert
{
    public static void That<TActual>(TActual actual, Constraint constraint)
        => constraint.ApplyTo(actual, null);

    public static void That<TActual>(TActual actual, Constraint constraint, string? message)
        => constraint.ApplyTo(actual, message);

    public static TException Throws<TException>(Action action)
        where TException : Exception
        => MSTestAssert.ThrowsException<TException>(action);

    public static void DoesNotThrow(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            throw new AssertFailedException($"Expected no exception, but got {ex.GetType().FullName}: {ex.Message}", ex);
        }
    }

    public static void Ignore(string? message = null)
        => throw new AssertInconclusiveException(message ?? "Ignored");

    public static void Fail(string? message = null)
        => MSTestAssert.Fail(message);
}

public static class Is
{
    public static Constraint True => new(actual => MSTestAssert.IsTrue(ToBool(actual)), "true");
    public static Constraint False => new(actual => MSTestAssert.IsFalse(ToBool(actual)), "false");
    public static Constraint Null => new(actual => MSTestAssert.IsNull(actual), "null");
    public static Constraint Empty => new(actual =>
    {
        switch (actual)
        {
            case null:
                MSTestAssert.Fail("Expected empty, but value was null.");
                break;
            case string text:
                MSTestAssert.AreEqual(string.Empty, text);
                break;
            case ICollection collection:
                MSTestAssert.AreEqual(0, collection.Count);
                break;
            case IEnumerable enumerable:
                MSTestAssert.IsFalse(enumerable.Cast<object?>().Any());
                break;
            default:
                MSTestAssert.Fail($"Expected empty, but value was {actual}.");
                break;
        }
    }, "empty");

    public static NotBuilder Not => new();

    public static Constraint EqualTo(object? expected) => new EqualConstraint(expected);
    public static Constraint SameAs(object? expected) => new(actual => MSTestAssert.AreSame(expected, actual), $"same as {expected}");
    public static Constraint TypeOf<TExpected>() => TypeOf(typeof(TExpected));
    public static Constraint TypeOf(Type expectedType) => new(actual =>
    {
        MSTestAssert.IsNotNull(actual);
        MSTestAssert.AreEqual(expectedType, actual!.GetType());
    }, $"type of {expectedType}");
    public static Constraint GreaterThan(IComparable expected) => new(actual => Compare(actual, expected, c => c > 0, "greater than"), $"greater than {expected}");
    public static Constraint GreaterThanOrEqualTo(IComparable expected) => new(actual => Compare(actual, expected, c => c >= 0, "greater than or equal to"), $"greater than or equal to {expected}");
    public static Constraint LessThanOrEqualTo(IComparable expected) => new(actual => Compare(actual, expected, c => c <= 0, "less than or equal to"), $"less than or equal to {expected}");

    private static bool ToBool(object? actual)
        => actual is bool value ? value : throw new AssertFailedException($"Expected bool, but got {actual?.GetType().FullName ?? "null"}.");

    private static void Compare(object? actual, IComparable expected, Func<int, bool> predicate, string relation)
    {
        if (actual is not IComparable comparable)
        {
            MSTestAssert.Fail($"Expected a comparable value {relation} {expected}, but got {actual?.GetType().FullName ?? "null"}.");
            return;
        }

        if (!predicate(comparable!.CompareTo(expected)))
            MSTestAssert.Fail($"Expected {actual} to be {relation} {expected}.");
    }
}

public static class Does
{
    public static Constraint Contain(object? expected) => new(actual =>
    {
        switch (actual)
        {
            case string text when expected is string expectedText:
                StringAssert.Contains(text, expectedText);
                break;
            case IEnumerable enumerable:
                MSTestAssert.IsTrue(enumerable.Cast<object?>().Contains(expected),
                    $"Expected collection to contain {expected}.");
                break;
            default:
                MSTestAssert.Fail($"Cannot apply Contains to {actual?.GetType().FullName ?? "null"}.");
                break;
        }
    }, $"contain {expected}");

    public static Constraint StartWith(string expected) => new(actual =>
    {
        MSTestAssert.IsInstanceOfType(actual, typeof(string));
        StringAssert.StartsWith((string)actual!, expected);
    }, $"start with {expected}");
}

public sealed class NotBuilder
{
    public Constraint Null => new(actual => MSTestAssert.IsNotNull(actual), "not null");
    public Constraint Empty => new(actual =>
    {
        switch (actual)
        {
            case string text:
                MSTestAssert.AreNotEqual(string.Empty, text);
                break;
            case ICollection collection:
                MSTestAssert.AreNotEqual(0, collection.Count);
                break;
            case IEnumerable enumerable:
                MSTestAssert.IsTrue(enumerable.Cast<object?>().Any());
                break;
            default:
                MSTestAssert.IsNotNull(actual);
                break;
        }
    }, "not empty");
}

public class Constraint
{
    private readonly Action<object?> _assertion;

    public Constraint(Action<object?> assertion, string description)
    {
        _assertion = assertion;
        _ = description;
    }

    public virtual void ApplyTo(object? actual, string? message)
    {
        try
        {
            _assertion(actual);
        }
        catch (AssertFailedException) when (!string.IsNullOrEmpty(message))
        {
            throw new AssertFailedException(message);
        }
    }

    public Constraint Within(double tolerance)
    {
        if (this is not EqualConstraint equal)
            throw new InvalidOperationException("Within can only be used with EqualTo.");

        equal.Tolerance = tolerance;
        return equal;
    }
}

internal sealed class EqualConstraint(object? expected) : Constraint(actual => ApplyEqual(actual, expected, null), $"equal to {expected}")
{
    public double? Tolerance { get; set; }

    public override void ApplyTo(object? actual, string? message)
        => ApplyEqual(actual, expected, Tolerance, message);

    private static void ApplyEqual(object? actual, object? expected, double? tolerance, string? message = null)
    {
        if (tolerance is not null && IsNumeric(actual) && IsNumeric(expected))
        {
            var actualDouble = Convert.ToDouble(actual, CultureInfo.InvariantCulture);
            var expectedDouble = Convert.ToDouble(expected, CultureInfo.InvariantCulture);
            MSTestAssert.AreEqual(expectedDouble, actualDouble, tolerance.Value, message);
            return;
        }

        if (IsNumeric(actual) && IsNumeric(expected))
        {
            var actualDouble = Convert.ToDouble(actual, CultureInfo.InvariantCulture);
            var expectedDouble = Convert.ToDouble(expected, CultureInfo.InvariantCulture);
            MSTestAssert.AreEqual(expectedDouble, actualDouble, message);
            return;
        }

        if (actual is IEnumerable actualEnumerable && expected is IEnumerable expectedEnumerable
            && actual is not string && expected is not string)
        {
            CollectionAssert.AreEqual(expectedEnumerable.Cast<object?>().ToArray(), actualEnumerable.Cast<object?>().ToArray(), message);
            return;
        }

        MSTestAssert.AreEqual(expected, actual, message);
    }

    private static bool IsNumeric(object? value)
        => value is byte or sbyte
            or short or ushort
            or int or uint
            or long or ulong
            or float or double or decimal;
}
