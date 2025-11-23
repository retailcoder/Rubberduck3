using Rubberduck.InternalApi.Execution.Values;

namespace Rubberduck.InternalApi.Execution;

public interface IExecutable<TValue> where TValue : VBTypedValue
{
    /// <summary>
    /// Executes the symbol and its children, in the given context.
    /// </summary>
    /// <returns>
    /// Returns a <c>TValue</c> representing the result of the expression; <c>null</c> if the symbol is a non-returning executable member.
    /// </returns>
    TValue? Execute(VBExecutionContext context, bool rethrow = false);
}

public interface IExecutable : IExecutable<VBTypedValue>
{
}
