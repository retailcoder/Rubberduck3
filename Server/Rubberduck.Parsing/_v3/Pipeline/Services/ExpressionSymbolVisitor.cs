using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model;
using Rubberduck.InternalApi.Model.Declarations.Execution;
using Rubberduck.InternalApi.Model.Declarations.Execution.Values;
using Rubberduck.InternalApi.Model.Declarations.Symbols;
using Rubberduck.Parsing.Grammar;
using System.Diagnostics;

namespace Rubberduck.Parsing._v3.Pipeline.Services;

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

    #region 5.4 Statements

    private Stack<List<ExecutableStatement>> _currentBlockStatements = [];
    private List<ExecutableStatement> _currentStatements => _currentBlockStatements.Count > 0 ? _currentBlockStatements.Peek() : [];

    private LineLabelSymbol? _currentLineLabel = default;

    #region 5.4.5 File statements
    public override ValuedExpression VisitOpenStmt([NotNull] VBAParser.OpenStmtContext context)
    {
        return base.VisitOpenStmt(context);
    }
    #endregion

    public override ValuedExpression VisitStatementLabelDefinition([NotNull] VBAParser.StatementLabelDefinitionContext context)
    {
        if (context.standaloneLineNumberLabel() is VBAParser.StandaloneLineNumberLabelContext lineNumberContext)
        {
            var lineNumber = lineNumberContext.lineNumberLabel().GetText();
            var symbol = _currentLineLabel = new LineLabelSymbol(lineNumber, _parentUri);
            _execution.AddLineLabel(symbol);
        }
        else if (context.identifierStatementLabel() is VBAParser.IdentifierStatementLabelContext labelContext)
        {
            var label = labelContext.legalLabelIdentifier().GetText();
            var symbol = _currentLineLabel = new LineLabelSymbol(label, _parentUri);
            _execution.AddLineLabel(symbol);
        }
        else if (context.combinedLabels() is VBAParser.CombinedLabelsContext combinedContext)
        {
            // since we have both, treat the line number as the primary/"current" label
            // because it will always appear first on that line.

            var lineNumber = combinedContext.lineNumberLabel().GetText();
            var symbol = _currentLineLabel = new LineLabelSymbol(lineNumber, _parentUri);
            _execution.AddLineLabel(symbol);

            var label = combinedContext.identifierStatementLabel().legalLabelIdentifier().GetText();
            _execution.AddLineLabel(new LineLabelSymbol(label, _parentUri));
        }

        return base.VisitStatementLabelDefinition(context);
    }

    public override ValuedExpression VisitModuleBodyElement([NotNull] VBAParser.ModuleBodyElementContext context)
    {
        Debug.Assert(_currentBlockStatements.Count == 0);
        _currentBlockStatements.Clear();

        return base.VisitModuleBodyElement(context);
    }

    public override ValuedExpression VisitBlock([NotNull] VBAParser.BlockContext context)
    {
        _currentBlockStatements.Push([]);
        var result = base.VisitBlock(context);

        // TODO issue diagnostic if block is empty
        return result;
    }

    public override ValuedExpression VisitGoToStmt([NotNull] VBAParser.GoToStmtContext context)
    {
        // expression is either a SimpleNameExpression or NumberLiteralExpression
        var expression = VisitExpression(context.expression());
        var labelName = expression.Name;

        var label = _execution.FindLineLabel(_parentUri, labelName);
        if (label is null)
        {
            // statement refers to an undefined label; issue compile error diagnostic
            label = new LineLabelSymbol(labelName, _parentUri);
            _execution.AddDiagnostic(RubberduckDiagnostic.CompileError(VBCompileErrorException.LabelNotDefined(label)));
        }

        // VBE normally makes them explicit
        var isImplicit = context.GOTO() is null;
        _currentStatements.Add(new GoToStatement(_parentUri, label, isImplicit));

        return expression;
    }

    public override ValuedExpression VisitSingleLineIfStmt([NotNull] VBAParser.SingleLineIfStmtContext context)
    {
        if (context.ifWithNonEmptyThen() is VBAParser.IfWithNonEmptyThenContext ifWithNonEmptyThen)
        {
            var expressionContext = ifWithNonEmptyThen.booleanExpression();
            if (ifWithNonEmptyThen.singleLineElseClause() is VBAParser.SingleLineElseClauseContext elseClauseContext)
            {
                _currentBlockStatements.Push([]);
                VisitListOrLabel(elseClauseContext.listOrLabel());
            }

            if (expressionContext is VBAParser.BooleanExpressionContext boolExpressionContext)
            {
                if (VisitBooleanExpression(boolExpressionContext) is BooleanValuedExpression condition)
                {
                    _currentBlockStatements.Push([]);
                    VisitListOrLabel(ifWithNonEmptyThen.listOrLabel());

                    _currentStatements.Add(new IfStatement(_parentUri, condition, _currentBlockStatements.Pop()));
                }
            }
        }
        else if (context.ifWithEmptyThen() is VBAParser.IfWithEmptyThenContext ifWithEmptyThen)
        {
            var expressionContext = ifWithEmptyThen.booleanExpression();
            if (expressionContext is VBAParser.BooleanExpressionContext boolExpressionContext)
            {
                if (VisitBooleanExpression(boolExpressionContext) is BooleanValuedExpression condition)
                {
                    if (ifWithEmptyThen.singleLineElseClause() is VBAParser.SingleLineElseClauseContext elseClauseContext)
                    {
                        _currentBlockStatements.Push([]);
                        VisitListOrLabel(elseClauseContext.listOrLabel());

                        _currentStatements.Add(new ElseStatement(_parentUri, _currentBlockStatements.Pop()));
                        // TODO support attaching diagnostics to statements
                        //_execution.AddDiagnostic(RubberduckDiagnostic.EmptyIfBlock(symbol));
                    }

                    _currentStatements.Add(new IfStatement(_parentUri, condition, _currentBlockStatements.Pop()));
                }
            }
        }

        return base.VisitSingleLineIfStmt(context);
    }

    public override ValuedExpression VisitIfStmt([NotNull] VBAParser.IfStmtContext context)
    {
        if (context.booleanExpression() is VBAParser.BooleanExpressionContext boolExpressionContext)
        {
            if (VisitBooleanExpression(boolExpressionContext) is BooleanValuedExpression condition)
            {
                VisitBlock(context.block());

                if (context.elseIfBlock() is VBAParser.ElseIfBlockContext[] elseIfBlockContexts)
                {
                    foreach (var elseIfBlockContext in elseIfBlockContexts)
                    {
                        if (elseIfBlockContext.booleanExpression() is VBAParser.BooleanExpressionContext elseIfBoolExpressionContext)
                        {
                            if (VisitBooleanExpression(elseIfBoolExpressionContext) is BooleanValuedExpression elseIfCondition)
                            {
                                _currentBlockStatements.Push([]);
                                VisitBlock(elseIfBlockContext.block());
                                _currentStatements.Add(new ElseIfStatement(_parentUri, elseIfCondition, _currentBlockStatements.Pop()));
                            }
                        }
                    }
                }

                if (context.elseBlock() is VBAParser.ElseBlockContext elseBlockContext)
                {
                    _currentBlockStatements.Push([]);
                    VisitBlock(elseBlockContext.block());

                    //if (_currentBlockStatements.Count == 0)
                    //{
                    //    _execution.AddDiagnostic(RubberduckDiagnostic.EmptyCodeBlock(TODO));
                    //}
                    _currentStatements.Add(new ElseStatement(_parentUri, _currentBlockStatements.Pop()));
                }

                _currentStatements.Add(new IfStatement(_parentUri, condition, _currentBlockStatements.Pop()));
            }
        }

        return base.VisitIfStmt(context);
    }

    public override ValuedExpression VisitDoBlockLoop([NotNull] VBAParser.DoBlockLoopContext context)
    {
        VisitBlock(context.block());
        _currentStatements.Add(new DoLoopStatement(_parentUri, _currentBlockStatements.Pop()));
        // TODO inspect to ensure there's an EXIT DO child, issue appropriate diagnostics
        return base.VisitDoBlockLoop(context);
    }

    public override ValuedExpression VisitDoBlockLoopWhileUntil([NotNull] VBAParser.DoBlockLoopWhileUntilContext context)
    {
        VisitBlock(context.block());
        if (VisitExpression(context.expression()) is BooleanValuedExpression condition)
        {
            if (context.WHILE() is not null)
            {
                _currentStatements.Add(new DoWhileLoopStatement(_parentUri, condition, _currentBlockStatements.Pop()));
            }
            else if (context.UNTIL() is not null)
            {
                _currentStatements.Add(new DoUntilLoopStatement(_parentUri, condition, _currentBlockStatements.Pop()));
            }
        }
        return base.VisitDoBlockLoopWhileUntil(context);
    }

    public override ValuedExpression VisitDoWhileUntilBlockLoop([NotNull] VBAParser.DoWhileUntilBlockLoopContext context)
    {
        VisitBlock(context.block());
        if (VisitExpression(context.expression()) is BooleanValuedExpression condition)
        {
            if (context.WHILE() is not null)
            {
                _currentStatements.Add(new LoopWhileDoStatement(_parentUri, condition, _currentBlockStatements.Pop()));
            }
            else if (context.UNTIL() is not null)
            {
                _currentStatements.Add(new LoopUntilDoStatement(_parentUri, condition, _currentBlockStatements.Pop()));
            }
        }
        return base.VisitDoWhileUntilBlockLoop(context);
    }

    public override ValuedExpression VisitWithStmt([NotNull] VBAParser.WithStmtContext context)
    {
        if (VisitExpression(context.expression()) is ValuedExpression expression)
        {
            VisitBlock(context.block());
            var target = new ObjectValuedExpression(_parentUri) { Children = expression.Children };
            _currentStatements.Add(new WithStatementSymbol(target, _parentUri, _currentBlockStatements.Pop()));
        }
        return base.VisitWithStmt(context);
    }

    private Stack<BlockStatement> _enteredSubBlocks = [];
    private Stack<BlockStatement> _enteredFunctionBlocks = [];
    private Stack<BlockStatement> _enteredPropertyBlocks = [];
    private Stack<BlockStatement> _enteredDoLoopBlocks = [];
    private Stack<BlockStatement> _enteredForLoopBlocks = [];
    public override ValuedExpression VisitExitStmt([NotNull] VBAParser.ExitStmtContext context)
    {
        if (context.EXIT_DO() is not null)
        {
            if (_enteredDoLoopBlocks.TryPop(out var binding))
            {
                _currentStatements.Add(new ExitStatement(_parentUri, binding, _currentLineLabel));
            }
            else
            {
                //_execution.AddDiagnostic(VBCompileErrorException.ExitDoNotWithinDoLoop());
            }
        }
        else if (context.EXIT_FOR() is not null)
        {
            if (_enteredForLoopBlocks.TryPop(out var binding))
            {
                _currentStatements.Add(new ExitStatement(_parentUri, binding, _currentLineLabel));
            }
            else
            {
                //_execution.AddDiagnostics(VBCompileErrorException.ExitForNotWithinForNext());
            }
        }
        else if (context.EXIT_SUB() is not null)
        {
            if (_enteredSubBlocks.TryPop(out var binding))
            {
                _currentStatements.Add(new ExitStatement(_parentUri, binding, _currentLineLabel));
            }
        }
        else if (context.EXIT_FUNCTION() is not null)
        {
            if (_enteredFunctionBlocks.TryPop(out var binding))
            {
                _currentStatements.Add(new ExitStatement(_parentUri, binding, _currentLineLabel));
            }
            else
            {
                //_execution.AddDiagnostics(VBCompileErrorException.ExitFunctionNotAllowedInSubOrProperty());
            }
        }
        else if (context.EXIT_PROPERTY() is not null)
        {
            if (_enteredPropertyBlocks.TryPop(out var binding))
            {
                _currentStatements.Add(new ExitStatement(_parentUri, binding, _currentLineLabel));
            }
            else
            {
                //_execution.AddDiagnostics(VBCompileErrorException.ExitPropertyNotAllowedInSubOrFunction);
            }
        }

        return base.VisitExitStmt(context);
    }

    public override ValuedExpression VisitRaiseEventStmt([NotNull] VBAParser.RaiseEventStmtContext context)
    {
        var identifier = context.identifier().GetText();

        return base.VisitRaiseEventStmt(context);
    }

    public override ValuedExpression VisitRedimStmt([NotNull] VBAParser.RedimStmtContext context)
    {
        if (context.redimDeclarationList()?.redimVariableDeclaration() is VBAParser.RedimVariableDeclarationContext[] variables)
        {
            foreach (var variable in variables)
            {
                var expression = variable.expression();

                if (expression is VBAParser.LExprContext lExpressionContext)
                {
                    var lExpression = lExpressionContext.lExpression();
                    if (lExpression is VBAParser.SimpleNameExprContext simpleName)
                    {

                        var name = simpleName.GetText();

                        if (variable.asTypeClause() is VBAParser.AsTypeClauseContext asTypeClause
                            && asTypeClause.type() is VBAParser.TypeContext typeContext)
                        {
                            // TODO get the VBType and subscripts
                            if (typeContext.LPAREN() is not null && typeContext.RPAREN() is not null)
                            {
                                // dynamic array declaration: "As Something()"
                            }
                        }
                    }
                    else if (lExpression is VBAParser.IndexExprContext indexExpr
                        && indexExpr.argumentList() is VBAParser.ArgumentListContext args)
                    {
                        // fixed-sized array declaration: "As Something(X To Y)"
                        foreach (var arg in args.argument())
                        {
                            if (arg.positionalArgument() is VBAParser.PositionalArgumentContext posArg)
                            {

                            }
                        }
                    }
                }
            }
        }

        return base.VisitRedimStmt(context);
    }
    #endregion

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
                    return new HexLiteralExpression(_parentUri, new VBLongValue().WithValue(hexValue));
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
                    return new OctalLiteralExpression(_parentUri, new VBLongValue().WithValue(octValue));
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
