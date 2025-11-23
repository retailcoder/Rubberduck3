using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model;
using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.Model.Declarations.Types.Abstract;
using Rubberduck.Parsing.Grammar;

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
