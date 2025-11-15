using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Microsoft.Extensions.Logging;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model;
using Rubberduck.InternalApi.Model.Declarations.Execution;
using Rubberduck.InternalApi.Model.Declarations.Execution.Values;
using Rubberduck.InternalApi.Model.Declarations.Symbols;
using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.Model.Declarations.Types.Abstract;
using Rubberduck.Parsing.Grammar;
using System.Runtime.CompilerServices;
using StringLiteralExpression = Rubberduck.InternalApi.Model.Declarations.Symbols.StringLiteralExpression;

namespace Rubberduck.Parsing._v3.Pipeline.Services;

public class IdentifierTypeVisitor : VBAParserBaseVisitor<VBType>
{
    private readonly WorkspaceUri _parentUri;
    private readonly VBExecutionContext _execution;
    public IdentifierTypeVisitor(WorkspaceUri parentUri, VBExecutionContext execution)
    {
        _parentUri = parentUri;
        _execution = execution;
    }

    private static readonly Dictionary<string, VBType> _keywordTypeMap = new()
    {
        { Tokens.Any, VBAnyType.TypeInfo },
        { Tokens.Boolean, VBBooleanType.TypeInfo },
        { Tokens.Byte, VBByteType.TypeInfo },
        { Tokens.Currency, VBCurrencyType.TypeInfo },
        { Tokens.Date, VBDateType.TypeInfo },
        { Tokens.Decimal, VBDecimalType.TypeInfo },
        { Tokens.Double, VBDoubleType.TypeInfo },
        { Tokens.Integer, VBIntegerType.TypeInfo },
        { Tokens.Long, VBLongType.TypeInfo },
        { Tokens.LongLong, VBLongLongType.TypeInfo },
        { Tokens.LongPtr, VBLongPtrType.TypeInfo },
        { Tokens.Single, VBSingleType.TypeInfo },
        { Tokens.String, VBStringType.TypeInfo },
        { Tokens.Object, VBObjectType.TypeInfo },
        { Tokens.Variant, VBVariantType.TypeInfo },
    };

    public override VBType VisitInstanceExpr([NotNull] VBAParser.InstanceExprContext context)
    {
        return _execution.ResolveType(Tokens.Me, _parentUri);
    }

    public override VBType VisitBaseType([NotNull] VBAParser.BaseTypeContext context)
    {
        return _keywordTypeMap[context.GetText()];
    }

    public override VBType VisitIdentifierValue([NotNull] VBAParser.IdentifierValueContext context)
    {
        var name = context.GetText();

        if (context.IDENTIFIER() is ITerminalNode identifierToken)
        {
            return _execution.ResolveType(identifierToken.GetText(), _parentUri);
        }
        else if (context.keyword() is VBAParser.KeywordContext keywordContext)
        {
            var keyword = keywordContext.GetText();
            if (_keywordTypeMap.TryGetValue(keyword, out var vbType))
            {
                return vbType;
            }
            return _execution.ResolveType(keyword, _parentUri);
        }

        return VBEmptyType.TypeInfo;
    }
}



public class ExpressionSymbolVisitor : VBAParserBaseVisitor<ValuedExpression>
{
    private readonly WorkspaceUri _parentUri;
    private readonly VBExecutionContext _execution;

    public ExpressionSymbolVisitor(WorkspaceUri parentUri, VBExecutionContext execution)
    {
        _parentUri = parentUri;
        _execution = execution;
    }

    private ValuedExpression InvalidExpression(ParserRuleContext context) => new InvalidExpression(context.GetText(), _parentUri);

    private Stack<VBAParser.TypeHintContext> _typeHintContext = [];
    #region 5.6 Expressions

    #region 5.6.5 Literal expressions
    public override ValuedExpression VisitBooleanLiteralIdentifier([NotNull] VBAParser.BooleanLiteralIdentifierContext context)
    {
        VBBooleanValue value;
        if (context.TRUE() != null)
        {
            value = VBBooleanValue.True;
        }
        else if (context.FALSE() != null)
        {
            value = VBBooleanValue.False;
        }
        else
        {
            return InvalidExpression(context);
        }

        return new BooleanLiteralExpression(_parentUri, value);
    }

    public override ValuedExpression VisitObjectLiteralIdentifier([NotNull] VBAParser.ObjectLiteralIdentifierContext context)
    {
        if (context.NOTHING() != null)
        {
            return new ObjectLiteralExpression(_parentUri);
        }
        else
        {
            return InvalidExpression(context);
        }
    }

    public override ValuedExpression VisitVariantLiteralIdentifier([NotNull] VBAParser.VariantLiteralIdentifierContext context)
    {
        if (context.EMPTY() != null)
        {
            return new EmptyLiteralExpression(_parentUri);
        }
        else if (context.NULL() != null)
        {
            return new NullLiteralExpression(_parentUri);
        }
        else
        {
            return InvalidExpression(context);
        }
    }

    public override ValuedExpression VisitNumberLiteral([NotNull] VBAParser.NumberLiteralContext context)
    {
        if (context.INTEGERLITERAL() != null)
        {
            if (long.TryParse(context.INTEGERLITERAL().GetText(), out var intValue))
            {
                if (_typeHintContext.Peek() is VBAParser.TypeHintContext typeHintContext)
                {
                    var hint = typeHintContext.GetText();
                    return hint switch
                    {
                        "%" => new IntegerLiteralExpression(_parentUri, new VBIntegerValue().WithValue((short)intValue)),
                        "&" => new LongLiteralExpression(_parentUri, new VBLongValue().WithValue(intValue)),
                        "^" => new LongLongLiteralExpression(_parentUri, new VBLongLongValue().WithValue(intValue)),
                        "$" => new StringLiteralExpression(_parentUri, new VBStringValue().WithValue(intValue.ToString())),
                        "!" => new SingleLiteralExpression(_parentUri, new VBSingleValue().WithValue(intValue)),
                        "#" => new DoubleLiteralExpression(_parentUri, new VBDoubleValue().WithValue(intValue)),
                        "@" => new DecimalLiteralExpression(_parentUri, new VBDecimalValue().WithValue(intValue)),
                        _ => InvalidExpression(typeHintContext),
                    };
                }
                if (intValue <= VBIntegerValue.MaxValue.Value || intValue >= VBIntegerValue.MinValue.Value)
                {
                    return new IntegerLiteralExpression(_parentUri, new VBIntegerValue().WithValue(intValue));
                }
                else
                {
                    return new LongLiteralExpression(_parentUri, new VBLongValue().WithValue(intValue));
                }
            }
            else
            {
                return InvalidExpression(context);
            }
        }
        else if (context.FLOATLITERAL() != null)
        {
            if (double.TryParse(context.FLOATLITERAL().GetText(), out var floatValue))
            {
                return new DoubleLiteralExpression(_parentUri, new VBDoubleValue().WithValue(floatValue));
            }
            else
            {
                return InvalidExpression(context);
            }
        }
        else if (context.HEXLITERAL() != null)
        {
            var hexText = context.HEXLITERAL().GetText().Substring(2); // remove &H
            if (long.TryParse(hexText, System.Globalization.NumberStyles.HexNumber, null, out var hexValue))
            {
                if (hexValue <= VBIntegerValue.MaxValue.Value || hexValue >= VBIntegerValue.MinValue.Value)
                {
                    return new HexLiteralExpression(_parentUri, new VBIntegerValue().WithValue(hexValue));
                }
                else
                {
                    return new HexLiteralExpression(_parentUri, new VBLongValue().WithValue(hexValue));
                }
            }
            else
            {
                return InvalidExpression(context);
            }
        }
        else if (context.OCTLITERAL() != null)
        {
            var octText = context.OCTLITERAL().GetText().Substring(2); // remove &O
            try
            {
                long octValue = 0;
                foreach (var c in octText)
                {
                    octValue = (octValue << 3) + (c - '0');
                }
                if (octValue <= VBIntegerValue.MaxValue.Value || octValue >= VBIntegerValue.MinValue.Value)
                {
                    return new OctalLiteralExpression(_parentUri, new VBIntegerValue().WithValue(octValue));
                }
                else
                {
                    return new OctalLiteralExpression(_parentUri, new VBLongValue().WithValue(octValue));
                }
            }
            catch
            {
                return InvalidExpression(context);
            }
        }
        else
        {
            return InvalidExpression(context);
        }
    }

    public override ValuedExpression VisitLiteralExpression([NotNull] VBAParser.LiteralExpressionContext context)
    {
        if (context.numberLiteral() is VBAParser.NumberLiteralContext numberLiteralContext)
        {
            return VisitNumberLiteral(numberLiteralContext);
        }
        if (context.literalIdentifier() is VBAParser.LiteralIdentifierContext literalIdentifierContext)
        {
            if (context.typeHint() is VBAParser.TypeHintContext typeHintContext)
            {
                _typeHintContext.Push(typeHintContext);
                var expression = VisitLiteralIdentifier(literalIdentifierContext);
                _typeHintContext.Pop();
                return expression;
            }
            else
            {
                return VisitLiteralIdentifier(literalIdentifierContext);
            }
        }
        else if (context.STRINGLITERAL() is ITerminalNode stringliteralToken)
        {
            var value = stringliteralToken.GetText().UnQuote();
            return new StringLiteralExpression(_parentUri, new VBStringValue().WithValue(value));
        }
        else if (context.DATELITERAL() is ITerminalNode dateLiteralToken)
        {
            var dateText = dateLiteralToken.GetText().Trim('#');
            if (DateTime.TryParse(dateText, out var dateValue))
            {
                return new InternalApi.Model.Declarations.Symbols.DateLiteralExpression(_parentUri, new VBDateValue().WithValue(dateValue));
            }
            else
            {
                return InvalidExpression(context);
            }
        }
        else
        {
            return InvalidExpression(context);
        }
    }
    #endregion

    public override ValuedExpression VisitParenthesizedExpr([NotNull] VBAParser.ParenthesizedExprContext context)
    {
        var inner = VisitExpression(context.expression());
        return new ParenthesizedExpression(_parentUri, inner);
    }

    #region operators
    public override ValuedExpression VisitTypeofexpr([NotNull] VBAParser.TypeofexprContext context)
    {
        return new TypeOfExpression(_parentUri, VisitExpression(context.expression()));
    }

    public override ValuedExpression VisitNewExpr([NotNull] VBAParser.NewExprContext context)
    {
        return VisitExpression(context.expression());
    }

    public override ValuedExpression VisitPowOp([NotNull] VBAParser.PowOpContext context)
    {
        var children = context.expression();
        var lhs = VisitExpression(children[0]);
        var rhs = VisitExpression(children[1]);
        return new PowOpExpression(_parentUri, lhs, rhs);
    }

    public override ValuedExpression VisitUnaryMinusOp([NotNull] VBAParser.UnaryMinusOpContext context)
    {
        var expression = VisitExpression(context.expression());
        return new UnaryMinusOpExpression(_parentUri, expression);
    }

    public override ValuedExpression VisitMultOp([NotNull] VBAParser.MultOpContext context)
    {
        var children = context.expression();
        var lhs = VisitExpression(children[0]);
        var rhs = VisitExpression(children[1]);

        if (context.MULT() is not null)
        {
            return new MultiplicationOpExpression(_parentUri, lhs, rhs);
        }

        if (context.DIV() is not null)
        {
            return new DivisionOpExpression(_parentUri, lhs, rhs);
        }

        return InvalidExpression(context);
    }

    public override ValuedExpression VisitIntDivOp([NotNull] VBAParser.IntDivOpContext context)
    {
        var children = context.expression();
        var lhs = VisitExpression(children[0]);
        var rhs = VisitExpression(children[1]);

        return new IntegerDivisionOpExpression(_parentUri, lhs, rhs);
    }

    public override ValuedExpression VisitModOp([NotNull] VBAParser.ModOpContext context)
    {
        var children = context.expression();
        var lhs = VisitExpression(children[0]);
        var rhs = VisitExpression(children[1]);

        return new ModulusOpExpression(_parentUri, lhs, rhs);
    }

    public override ValuedExpression VisitAddOp([NotNull] VBAParser.AddOpContext context)
    {
        var children = context.expression();
        var lhs = VisitExpression(children[0]);
        var rhs = VisitExpression(children[1]);

        if (context.PLUS() is not null)
        {
            return new AddOpExpression(_parentUri, lhs, rhs);
        }

        if (context.MINUS() is not null)
        {
            return new SubtractOpExpression(_parentUri, lhs, rhs);
        }

        return InvalidExpression(context);
    }

    public override ValuedExpression VisitConcatOp([NotNull] VBAParser.ConcatOpContext context)
    {
        var children = context.expression();
        var lhs = VisitExpression(children[0]);
        var rhs = VisitExpression(children[1]);

        return new ConcatOpExpression(_parentUri, lhs, rhs);
    }

    public override ValuedExpression VisitRelationalOp([NotNull] VBAParser.RelationalOpContext context)
    {
        var children = context.expression();
        var lhs = VisitExpression(children[0]);
        var rhs = VisitExpression(children[1]);

        if (context.EQ() is not null)
        {
            return new EqCompareOpExpression(_parentUri, lhs, rhs);
        }

        if (context.NEQ() is not null)
        {
            return new NeqCompareOpExpression(_parentUri, lhs, rhs);
        }

        if (context.LT() is not null)
        {
            return new LtCompareOpExpression(_parentUri, lhs, rhs);
        }

        if (context.LEQ() is not null)
        {
            return new LEqCompareOpExpression(_parentUri, lhs, rhs);
        }

        if (context.GEQ() is not null)
        {
            return new GEqCompareOpExpression(_parentUri, lhs, rhs);
        }

        if (context.LIKE() is not null)
        {
            return new LikeCompareOpExpression(_parentUri, lhs, rhs);
        }

        if (context.IS() is not null)
        {
            return new IsCompareOpExpression(_parentUri, lhs, rhs);
        }

        return InvalidExpression(context);
    }

    public override ValuedExpression VisitLogicalNotOp([NotNull] VBAParser.LogicalNotOpContext context)
    {
        return new UnaryNotOpExpression(_parentUri, VisitExpression(context.expression()));
    }

    public override ValuedExpression VisitLogicalAndOp([NotNull] VBAParser.LogicalAndOpContext context)
    {
        var children = context.expression();
        var lhs = VisitExpression(children[0]);
        var rhs = VisitExpression(children[1]);
        return new LogicalAndOpExpression(_parentUri, lhs, rhs);
    }

    public override ValuedExpression VisitLogicalOrOp([NotNull] VBAParser.LogicalOrOpContext context)
    {
        var children = context.expression();
        var lhs = VisitExpression(children[0]);
        var rhs = VisitExpression(children[1]);
        return new LogicalOrOpExpression(_parentUri, lhs, rhs);
    }

    public override ValuedExpression VisitLogicalXorOp([NotNull] VBAParser.LogicalXorOpContext context)
    {
        var children = context.expression();
        var lhs = VisitExpression(children[0]);
        var rhs = VisitExpression(children[1]);
        return new LogicalXOrOpExpression(_parentUri, lhs, rhs);
    }

    public override ValuedExpression VisitLogicalEqvOp([NotNull] VBAParser.LogicalEqvOpContext context)
    {
        var children = context.expression();
        var lhs = VisitExpression(children[0]);
        var rhs = VisitExpression(children[1]);
        return new LogicalEqvOpExpression(_parentUri, lhs, rhs);
    }

    public override ValuedExpression VisitLogicalImpOp([NotNull] VBAParser.LogicalImpOpContext context)
    {
        var children = context.expression();
        var lhs = VisitExpression(children[0]);
        var rhs = VisitExpression(children[1]);
        return new LogicalImpOpExpression(_parentUri, lhs, rhs);
    }
    #endregion


    public override ValuedExpression VisitIdentifierValue([NotNull] VBAParser.IdentifierValueContext context)
    {
        if (context.IDENTIFIER() is ITerminalNode identifierToken)
        {
            // we resolve the identifier to a VBType if it's used in a TypeOf expression
            if (context.GetAncestor<VBAParser.TypeofexprContext>() is VBAParser.TypeofexprContext ||
                context.GetAncestor<VBAParser.CtTypeofexprContext>() is VBAParser.CtTypeofexprContext)
            {
                var vbType = _execution.ResolveType(identifierToken.GetText(), _parentUri);
                return new TypeDescValuedExpression(_parentUri, vbType);
            }
        }
        else if (context.keyword() is VBAParser.KeywordContext keywordContext)
        {
            // identifier is a reserved keyword (may or may not be valid)
            return VisitKeyword(keywordContext);
        }
        else if (context.foreignName() is VBAParser.ForeignNameContext foreignNameContext)
        {
            // square-bracketed name expression
        }

        return base.VisitIdentifierValue(context);
    }
    #endregion
}

public interface IVBListener<TResult> : IVBAParserListener
{
    TResult Result { get; }
}

public class MemberSymbolsListener : VBAParserBaseListener, IVBListener<Symbol>
{
    private readonly ILogger _logger;
    private readonly WorkspaceFileUri _workspaceFileUri;
    private readonly bool _isVB6 = false;

    private bool _hasModuleHeader;
    private bool _isMultiUse = true;

    private string _vbNameAttributeValue = null!;
    private bool _isGlobalNamespace = false;
    private bool _isCreatable = false;
    private bool _isPredeclaredId = false;
    private bool _isExposed = false;

    /// <summary>
    /// Accumulates symbols to be added to the current context.
    /// </summary>
    /// <remarks>
    /// Push an empty list when entering a new context; Pop the child symbols list when creating the current symbol when exiting the context.
    /// </remarks>
    private readonly Stack<List<(Type TContext, Symbol Symbol)>> _currentSymbolChildren = [];
    private readonly Stack<WorkspaceFileUri> _currentParentUri = [];
    private Stack<Type> _currentContextType = [];

    // track parameter positions so we can correctly identify property let/set value parameters
    // and validate that ParamArray and optional parameters are last
    private int _currentContextParameterCount = 0;
    private int _currentParameterIndex = 0;
    private List<VBAParser.ArgContext> _currentContextParameters = [];

    private string ModuleName => string.IsNullOrWhiteSpace(_vbNameAttributeValue)
        ? _workspaceFileUri.FileNameWithoutExtension
        : _vbNameAttributeValue;

    private void LogStackState([CallerMemberName] string? name = null, bool isBefore = true)
    {
        //if (!_currentContextType.TryPeek(out var currentContextType))
        //{
        //    currentContextType = null;
        //}
        //if (!_currentParentUri.TryPeek(out var currentParentUri))
        //{
        //    currentParentUri = null;
        //}
        //if (!_currentSymbolChildren.TryPeek(out var currentSymbolChildren))
        //{
        //    currentSymbolChildren = null;
        //}

        //_logger.LogTrace($"Thread {Environment.CurrentManagedThreadId} [{name}] ({(isBefore ? "before" : "after")}) | ContentType: {currentContextType?.Name ?? "(top of stack)"} (stack depth: {_currentContextType.Count}) | CurrentParentUri: {currentParentUri?.ToString() ?? "(top of stack)"} (stack depth: {_currentParentUri.Count}) | CurrentSymbolChildren: {currentSymbolChildren?.Count.ToString() ?? "(top of stack)"} (stack depth: {_currentSymbolChildren.Count})");
    }

    public MemberSymbolsListener(ILogger logger, WorkspaceFileUri uri, bool isVB6 = false)
    {
        _logger = logger;
        _workspaceFileUri = uri;
        _isVB6 = isVB6;
    }

    public Symbol Result { get; private set; } = null!;

    private void OnChildSymbol<TContext>(Symbol symbol, TContext _) where TContext : ParserRuleContext
    {
        _currentSymbolChildren.Peek().Add((typeof(TContext), symbol));
        LogStackState(isBefore: false);
    }

    private void OnEnterNewCurrentSymbol<TContext>(TContext context) where TContext : ParserRuleContext
    {
        LogStackState(isBefore: true);

        var newParentUri = _workspaceFileUri;
        if (_currentParentUri.TryPeek(out var uri))
        {
            newParentUri = uri;
        }

        newParentUri = newParentUri.GetChildSymbolUri($"{typeof(TContext).Name}@{context.Start.StartIndex}") ?? _workspaceFileUri;

        _currentParentUri.Push(newParentUri);
        _currentSymbolChildren.Push([]);
        _currentContextType.Push(typeof(TContext));

        _currentParameterIndex = 0;
        _currentContextParameters.Clear();
        _currentContextParameterCount = context switch
        {
            VBAParser.FunctionStmtContext func => func.argList()?.arg().Length ?? 0,
            VBAParser.SubStmtContext sub => sub.argList()?.arg().Length ?? 0,
            VBAParser.PropertyGetStmtContext propGet => propGet.argList()?.arg().Length ?? 0,
            VBAParser.PropertyLetStmtContext propLet => propLet.argList()?.arg().Length ?? 0,
            VBAParser.PropertySetStmtContext propSet => propSet.argList()?.arg().Length ?? 0,
            VBAParser.DeclareStmtContext declare => declare.argList()?.arg().Length ?? 0,
            VBAParser.EventStmtContext eventStmt => eventStmt.argList()?.arg().Length ?? 0,
            _ => 0
        };

        LogStackState(isBefore: false);
    }

    private Symbol CreateCurrentSymbol<TContext>(Func<IEnumerable<Symbol>, Symbol> create, TContext context) where TContext : ParserRuleContext
    {
        LogStackState(isBefore: true);

        var type = _currentContextType.Pop();
        if (type != null && typeof(TContext) != type)
        {
            throw new InvalidOperationException($"***Thread {Environment.CurrentManagedThreadId} BUG: Expected current symbol under current context type {type.Name ?? "(null)"}, but context is {typeof(TContext).Name}.");
        }

        _currentParentUri.Pop();

        var children = _currentSymbolChildren.Pop()
            .Select(node => node.Symbol.WithParentUri(_currentParentUri.TryPeek(out var parentUri) ? parentUri : _workspaceFileUri))
            .ToArray();

        var symbol = create(children);

        LogStackState(isBefore: false);
        return symbol;
    }

    public override void EnterModule([NotNull] VBAParser.ModuleContext context) => OnEnterNewCurrentSymbol(context);

    public override void ExitModule([NotNull] VBAParser.ModuleContext context)
    {
        LogStackState(isBefore: true);
        Symbol moduleSymbol;

        if (_hasModuleHeader)
        {
            // if it has a header, it's a class module
            foreach (var element in context.moduleConfig().moduleConfigElement())
            {
                var name = GetIdentifierNameTokenText(element.unrestrictedIdentifier());
                if (string.Equals(name, "MultiUse", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (element.expression() is VBAParser.LiteralExprContext literal)
                    {
                        var value = int.Parse(literal.literalExpression()?.numberLiteral().GetText() ?? "0");
                        _isMultiUse = value != 0;
                    }
                }
            }

            moduleSymbol = CreateCurrentSymbol(children => new ClassModuleSymbol(EvaluateClassInstancingMode(), ModuleName, _workspaceFileUri, children, _isPredeclaredId, isUserDefined: true), context);
        }
        else
        {
            // otherwise, it's a standard module
            moduleSymbol = CreateCurrentSymbol(children => new StandardModuleSymbol(ModuleName, _workspaceFileUri, children), context);
        }

        Result = moduleSymbol;
        LogStackState(isBefore: false);
    }

    public override void ExitModuleAttributes([NotNull] VBAParser.ModuleAttributesContext context)
    {
        var comparison = StringComparison.InvariantCultureIgnoreCase;
        foreach (var attribute in context.attributeStmt())
        {
            var name = attribute.attributeName().GetText();
            var value = attribute.attributeValue()[0].GetText();

            if (string.Equals(Tokens.VB_Name, name, comparison))
            {
                _vbNameAttributeValue = value.UnQuote();
            }
            else if (string.Equals(Tokens.VB_GlobalNameSpace, name, comparison))
            {
                _isGlobalNamespace = string.Equals(Tokens.True, value, comparison);
            }
            else if (string.Equals(Tokens.VB_Creatable, name, comparison))
            {
                _isCreatable = string.Equals(Tokens.True, value, comparison);
            }
            else if (string.Equals(Tokens.VB_PredeclaredId, name, comparison))
            {
                _isPredeclaredId = string.Equals(Tokens.True, value, comparison);
            }
            else if (string.Equals(Tokens.VB_Exposed, name, comparison))
            {
                _isExposed = string.Equals(Tokens.True, value, comparison);
            }
        }
    }

    public override void ExitModuleHeader([NotNull] VBAParser.ModuleHeaderContext context) => _hasModuleHeader = context.ChildCount > 0;

    protected Instancing EvaluateClassInstancingMode()
    {
        if (!_isVB6)
        {
            // VBA classes are either private or public but not creatable (regardless of VB_GlobalNameSpace and/or VB_Creatable):
            return _isExposed ? Instancing.PublicNotCreatable : Instancing.Private;
        }

        if (_isExposed)
        {
            if (!_isCreatable)
            {
                return Instancing.PublicNotCreatable;
            }

            if (!_isGlobalNamespace)
            {
                return _isMultiUse ? Instancing.MultiUse : Instancing.SingleUse;
            }

            return _isMultiUse ? Instancing.GlobalMultiUse : Instancing.GlobalSingleUse;
        }

        return Instancing.Private;
    }

    protected InternalApi.Model.Accessibility GetAccessibility(VBAParser.VisibilityContext context)
    {
        if (context.PRIVATE() != null)
        {
            return InternalApi.Model.Accessibility.Private;
        }

        if (context.PUBLIC() != null)
        {
            return InternalApi.Model.Accessibility.Public;
        }

        if (context.FRIEND() != null)
        {
            return InternalApi.Model.Accessibility.Friend;
        }

        if (context.GLOBAL() != null)
        {
            return InternalApi.Model.Accessibility.Global;
        }

        return InternalApi.Model.Accessibility.Implicit;
    }

    protected ParameterModifier GetModifier(VBAParser.ArgContext context)
    {
        var isByRef = context.BYREF() != null;

        if (context.BYVAL() != null)
        {
            // ByVal modifier is explicitly specified
            return ParameterModifier.ExplicitByVal;
        }
        else if (_currentContextType.Peek() == typeof(VBAParser.PropertyLetStmtContext)
              || _currentContextType.Peek() == typeof(VBAParser.PropertySetStmtContext))
        {
            var isLast = _currentParameterIndex == _currentContextParameterCount - 1;
            if (isLast)
            {
                // the last parameter of a Property Let/Set is always ByVal, explicit or not.
                return ParameterModifier.ImplicitByVal;
            }
            else
            {
                // other parameters follow normal ByRef/ByVal rules
                return isByRef
                    ? ParameterModifier.ExplicitByRef
                    : ParameterModifier.ImplicitByRef;
            }
        }
        else if (isByRef)
        {
            // ByRef modifier is explicitly specified
            return ParameterModifier.ExplicitByRef;
        }

        // if no modifier is specified, it's otherwise implicitly ByRef
        return ParameterModifier.ImplicitByRef;
    }

    protected string GetIdentifierNameTokenText(VBAParser.UnrestrictedIdentifierContext context)
        => GetIdentifierNameTokenText(context.identifier());
    protected string GetIdentifierNameTokenText(VBAParser.IdentifierContext context)
    {
        if (context.typedIdentifier() is null)
        {
            return GetIdentifierNameTokenText(context.untypedIdentifier());
        }

        return GetIdentifierNameTokenText(context.typedIdentifier());
    }
    protected string GetIdentifierNameTokenText(VBAParser.TypedIdentifierContext context)
        => GetIdentifierNameTokenText(context.untypedIdentifier());
    protected string GetIdentifierNameTokenText(VBAParser.UntypedIdentifierContext context)
        => GetIdentifierNameTokenText(context.identifierValue());
    protected string GetIdentifierNameTokenText(VBAParser.IdentifierValueContext context)
        => context.IDENTIFIER()?.GetText() ?? context.keyword().GetText();

    protected string? GetAsTypeExpressionText(VBAParser.AsTypeClauseContext? context)
        => context?.type().GetText();

    public override void ExitArg([NotNull] VBAParser.ArgContext context)
    {
        string name = GetIdentifierNameTokenText(context.unrestrictedIdentifier());
        var asTypeExpression = GetAsTypeExpressionText(context.asTypeClause());
        var modifier = GetModifier(context);

        var parentUri = _workspaceFileUri.GetChildSymbolUri(name);
        var isLast = _currentParameterIndex == _currentContextParameterCount - 1;

        ParameterSymbol symbol;
        if (context.OPTIONAL() != null)
        {
            var valueExpression = context.argDefaultValue()?.expression().GetText();
            symbol = new OptionalParameterSymbol(name, parentUri, modifier, asTypeExpression, valueExpression)
            {
                Position = _currentParameterIndex,
                IsLast = isLast
            };
        }
        else if (context.PARAMARRAY() != null)
        {
            symbol = new ParamArrayParameterSymbol(name, parentUri, asTypeExpression)
            {
                Position = _currentParameterIndex,
                IsLast = isLast
            };
        }
        else
        {
            symbol = new ParameterSymbol(name, parentUri, modifier, asTypeExpression)
            {
                Position = _currentParameterIndex,
                IsLast = isLast
            };
        }

        _currentParameterIndex++;
        _currentContextParameters.Add(context);

        OnChildSymbol(symbol, context);
    }

    public override void EnterDeclareStmt([NotNull] VBAParser.DeclareStmtContext context) => OnEnterNewCurrentSymbol(context);

    public override void ExitDeclareStmt([NotNull] VBAParser.DeclareStmtContext context)
    {
        var isPtrSafe = context.PTRSAFE() != null;
        var name = context.identifier().GetText();
        var accessibility = GetAccessibility(context.visibility());
        var asTypeExpression = GetAsTypeExpressionText(context.asTypeClause());

        var library = context.STRINGLITERAL()[0].GetText().UnQuote();

        string? alias = null;
        if (context.ALIAS() != null)
        {
            alias = context.STRINGLITERAL()[1].GetText().UnQuote();
        }

        if (context.FUNCTION() != null)
        {
            OnChildSymbol(CreateCurrentSymbol(children =>
                new LibraryFunctionImportSymbol(library, name, alias, isPtrSafe, _workspaceFileUri, accessibility, children.OfType<ParameterSymbol>(), asTypeExpression), context), context);
        }
        else if (context.SUB() != null)
        {
            OnChildSymbol(CreateCurrentSymbol(children =>
                new LibraryProcedureImportSymbol(library, name, alias, isPtrSafe, _workspaceFileUri, accessibility, children.OfType<ParameterSymbol>()), context), context);
        }
    }

    public override void EnterEventStmt([NotNull] VBAParser.EventStmtContext context) => OnEnterNewCurrentSymbol(context);
    public override void ExitEventStmt([NotNull] VBAParser.EventStmtContext context)
    {
        var name = context.identifier().GetText();
        var accessibility = GetAccessibility(context.visibility());

        OnChildSymbol(CreateCurrentSymbol(children => new EventSymbol(name, _workspaceFileUri, accessibility, children.OfType<ParameterSymbol>()), context), context);
    }

    public override void EnterEnumerationStmt([NotNull] VBAParser.EnumerationStmtContext context) => OnEnterNewCurrentSymbol(context);

    public override void ExitEnumerationStmt([NotNull] VBAParser.EnumerationStmtContext context)
    {
        var name = context.identifier().GetText();
        var accessibility = GetAccessibility(context.visibility());

        OnChildSymbol(CreateCurrentSymbol(children => new EnumSymbol(name, _workspaceFileUri, accessibility, children.OfType<EnumMemberSymbol>()), context), context);
    }

    public override void EnterUdtDeclaration([NotNull] VBAParser.UdtDeclarationContext context) => OnEnterNewCurrentSymbol(context);
    public override void ExitUdtDeclaration([NotNull] VBAParser.UdtDeclarationContext context)
    {
        var name = GetIdentifierNameTokenText(context.untypedIdentifier());
        var accessibility = GetAccessibility(context.visibility());

        OnChildSymbol(CreateCurrentSymbol(children => new UserDefinedTypeSymbol(name, _workspaceFileUri, accessibility, children.OfType<UserDefinedTypeMemberSymbol>()), context), context);
    }

    public override void ExitEnumerationStmt_Constant([NotNull] VBAParser.EnumerationStmt_ConstantContext context)
    {
        var name = context.identifier().GetText();
        var valueExpression = context.expression()?.GetText();

        OnChildSymbol(new EnumMemberSymbol(name, _workspaceFileUri, valueExpression), context);
    }
    public override void ExitUdtMember([NotNull] VBAParser.UdtMemberContext context)
    {
        string name;
        string? asTypeExpression;

        var nameContext = context.reservedNameMemberDeclaration();
        if (nameContext != null)
        {
            name = GetIdentifierNameTokenText(nameContext.unrestrictedIdentifier());
            asTypeExpression = GetAsTypeExpressionText(nameContext.asTypeClause());
        }
        else
        {
            var declaration = context.untypedNameMemberDeclaration();
            name = GetIdentifierNameTokenText(declaration.untypedIdentifier());
            asTypeExpression = GetAsTypeExpressionText(declaration.optionalArrayClause()?.asTypeClause());
        }

        OnChildSymbol(new UserDefinedTypeMemberSymbol(name, _workspaceFileUri, asTypeExpression), context);
    }

    public override void EnterSubStmt([NotNull] VBAParser.SubStmtContext context) => OnEnterNewCurrentSymbol(context);

    public override void ExitSubStmt([NotNull] VBAParser.SubStmtContext context)
    {
        var name = GetIdentifierNameTokenText(context.subroutineName().identifier());
        var accessibility = GetAccessibility(context.visibility());

        OnChildSymbol(CreateCurrentSymbol(children => new ProcedureSymbol(name, _workspaceFileUri, accessibility, children), context), context);
    }

    public override void EnterFunctionStmt([NotNull] VBAParser.FunctionStmtContext context) => OnEnterNewCurrentSymbol(context);

    public override void ExitFunctionStmt([NotNull] VBAParser.FunctionStmtContext context)
    {
        var name = GetIdentifierNameTokenText(context.functionName().identifier());
        var accessibility = GetAccessibility(context.visibility());
        var typeName = GetAsTypeExpressionText(context.asTypeClause());

        OnChildSymbol(CreateCurrentSymbol(children => new FunctionSymbol(name, _workspaceFileUri, accessibility, children, typeName), context), context);
    }

    public override void EnterPropertyGetStmt([NotNull] VBAParser.PropertyGetStmtContext context) => OnEnterNewCurrentSymbol(context);

    public override void ExitPropertyGetStmt([NotNull] VBAParser.PropertyGetStmtContext context)
    {
        var name = GetIdentifierNameTokenText(context.functionName().identifier());
        var accessibility = GetAccessibility(context.visibility());
        var typeName = GetAsTypeExpressionText(context.asTypeClause());

        OnChildSymbol(CreateCurrentSymbol(children => new PropertyGetSymbol(name, _workspaceFileUri, accessibility, children, typeName), context), context);
    }

    public override void EnterPropertyLetStmt([NotNull] VBAParser.PropertyLetStmtContext context) => OnEnterNewCurrentSymbol(context);

    public override void ExitPropertyLetStmt([NotNull] VBAParser.PropertyLetStmtContext context)
    {
        var name = GetIdentifierNameTokenText(context.subroutineName().identifier());
        var accessibility = GetAccessibility(context.visibility());

        OnChildSymbol(CreateCurrentSymbol(children => new PropertyLetSymbol(name, _workspaceFileUri, accessibility, children), context), context);
    }

    public override void EnterPropertySetStmt([NotNull] VBAParser.PropertySetStmtContext context) => OnEnterNewCurrentSymbol(context);

    public override void ExitPropertySetStmt([NotNull] VBAParser.PropertySetStmtContext context)
    {
        var name = GetIdentifierNameTokenText(context.subroutineName().identifier());
        var accessibility = GetAccessibility(context.visibility());

        OnChildSymbol(CreateCurrentSymbol(children => new PropertySetSymbol(name, _workspaceFileUri, accessibility, children), context), context);
    }
}
