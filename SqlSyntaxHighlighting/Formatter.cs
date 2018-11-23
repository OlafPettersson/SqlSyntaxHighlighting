using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using PoorMansTSqlFormatterLib;
using PoorMansTSqlFormatterLib.Interfaces;

namespace ConsoleApplication1
{
    public class CustomFormatter
    {
        public CustomFormatter()
        {
            ErrorOutputPrefix = MessagingConstants.FormatErrorDefaultMessage + Environment.NewLine;
        }

        public string ErrorOutputPrefix { get; set; }

        private static Regex VariableRegex = new Regex(@"^(?:@@?[a-zA-Z_][a-zA-Z0-9_]*@?@?|[?](?:[a-zA-Z_][a-zA-Z0-9_]*)?)$");

        public IEnumerable<KeyValuePair<string, string>> FormatSQLTree(XmlDocument sqlTreeDoc)
        {
            string rootElement = SqlStructureConstants.ENAME_SQL_ROOT;
            BaseFormatterState state = new BaseFormatterState();

            if (sqlTreeDoc.SelectSingleNode(string.Format("/{0}/@{1}[.=1]", rootElement, SqlStructureConstants.ANAME_ERRORFOUND)) != null)
                state.AddOutputContent(ErrorOutputPrefix);

            XmlNodeList rootList = sqlTreeDoc.SelectNodes(string.Format("/{0}/*", rootElement));
            return FormatSQLNodes(rootList, state);
        }

        public IEnumerable<KeyValuePair<string, string>> FormatSQLTree(XmlNode sqlTreeFragment)
        {
            BaseFormatterState state = new BaseFormatterState();
            return FormatSQLNodes(sqlTreeFragment.SelectNodes("."), state);
        }

        private static IEnumerable<KeyValuePair<string, string>> FormatSQLNodes(XmlNodeList nodes, BaseFormatterState state)
        {
            ProcessSqlNodeList(state, nodes);
            return state.DumpOutput();
        }

        private static void ProcessSqlNodeList(BaseFormatterState state, XmlNodeList rootList)
        {
            foreach (XmlElement contentElement in rootList)
                ProcessSqlNode(state, contentElement);
        }

        private static void ProcessSqlNode(BaseFormatterState state, XmlElement contentElement)
        {
            if (contentElement.GetAttribute(SqlStructureConstants.ANAME_HASERROR) == "1")
                state.OpenClass(SqlHtmlConstants.CLASS_ERRORHIGHLIGHT);

            switch (contentElement.Name)
            {
                case SqlStructureConstants.ENAME_DDLDETAIL_PARENS:
                case SqlStructureConstants.ENAME_DDL_PARENS:
                case SqlStructureConstants.ENAME_FUNCTION_PARENS:
                case SqlStructureConstants.ENAME_IN_PARENS:
                case SqlStructureConstants.ENAME_EXPRESSION_PARENS:
                case SqlStructureConstants.ENAME_SELECTIONTARGET_PARENS:
                    state.AddOutputContent("(");
                    ProcessSqlNodeList(state, contentElement.SelectNodes("*"));
                    state.AddOutputContent(")");
                    break;

                case SqlStructureConstants.ENAME_SQL_ROOT:
                case SqlStructureConstants.ENAME_SQL_STATEMENT:
                case SqlStructureConstants.ENAME_SQL_CLAUSE:
                case SqlStructureConstants.ENAME_BOOLEAN_EXPRESSION:
                case SqlStructureConstants.ENAME_DDL_PROCEDURAL_BLOCK:
                case SqlStructureConstants.ENAME_DDL_OTHER_BLOCK:
                case SqlStructureConstants.ENAME_DDL_DECLARE_BLOCK:
                case SqlStructureConstants.ENAME_CURSOR_DECLARATION:
                case SqlStructureConstants.ENAME_BEGIN_END_BLOCK:
                case SqlStructureConstants.ENAME_TRY_BLOCK:
                case SqlStructureConstants.ENAME_CATCH_BLOCK:
                case SqlStructureConstants.ENAME_CASE_STATEMENT:
                case SqlStructureConstants.ENAME_CASE_INPUT:
                case SqlStructureConstants.ENAME_CASE_WHEN:
                case SqlStructureConstants.ENAME_CASE_THEN:
                case SqlStructureConstants.ENAME_CASE_ELSE:
                case SqlStructureConstants.ENAME_IF_STATEMENT:
                case SqlStructureConstants.ENAME_ELSE_CLAUSE:
                case SqlStructureConstants.ENAME_WHILE_LOOP:
                case SqlStructureConstants.ENAME_DDL_AS_BLOCK:
                case SqlStructureConstants.ENAME_BETWEEN_CONDITION:
                case SqlStructureConstants.ENAME_BETWEEN_LOWERBOUND:
                case SqlStructureConstants.ENAME_BETWEEN_UPPERBOUND:
                case SqlStructureConstants.ENAME_CTE_WITH_CLAUSE:
                case SqlStructureConstants.ENAME_CTE_ALIAS:
                case SqlStructureConstants.ENAME_CTE_AS_BLOCK:
                case SqlStructureConstants.ENAME_CURSOR_FOR_BLOCK:
                case SqlStructureConstants.ENAME_CURSOR_FOR_OPTIONS:
                case SqlStructureConstants.ENAME_TRIGGER_CONDITION:
                case SqlStructureConstants.ENAME_COMPOUNDKEYWORD:
                case SqlStructureConstants.ENAME_BEGIN_TRANSACTION:
                case SqlStructureConstants.ENAME_ROLLBACK_TRANSACTION:
                case SqlStructureConstants.ENAME_SAVE_TRANSACTION:
                case SqlStructureConstants.ENAME_COMMIT_TRANSACTION:
                case SqlStructureConstants.ENAME_BATCH_SEPARATOR:
                case SqlStructureConstants.ENAME_SET_OPERATOR_CLAUSE:
                case SqlStructureConstants.ENAME_CONTAINER_OPEN:
                case SqlStructureConstants.ENAME_CONTAINER_MULTISTATEMENT:
                case SqlStructureConstants.ENAME_CONTAINER_SINGLESTATEMENT:
                case SqlStructureConstants.ENAME_CONTAINER_GENERALCONTENT:
                case SqlStructureConstants.ENAME_CONTAINER_CLOSE:
                case SqlStructureConstants.ENAME_SELECTIONTARGET:
                case SqlStructureConstants.ENAME_PERMISSIONS_BLOCK:
                case SqlStructureConstants.ENAME_PERMISSIONS_DETAIL:
                case SqlStructureConstants.ENAME_PERMISSIONS_TARGET:
                case SqlStructureConstants.ENAME_PERMISSIONS_RECIPIENT:
                case SqlStructureConstants.ENAME_DDL_WITH_CLAUSE:
                case SqlStructureConstants.ENAME_MERGE_CLAUSE:
                case SqlStructureConstants.ENAME_MERGE_TARGET:
                case SqlStructureConstants.ENAME_MERGE_USING:
                case SqlStructureConstants.ENAME_MERGE_CONDITION:
                case SqlStructureConstants.ENAME_MERGE_WHEN:
                case SqlStructureConstants.ENAME_MERGE_THEN:
                case SqlStructureConstants.ENAME_MERGE_ACTION:
                case SqlStructureConstants.ENAME_JOIN_ON_SECTION:
                    foreach (XmlNode childNode in contentElement.ChildNodes)
                    {
                        switch (childNode.NodeType)
                        {
                            case XmlNodeType.Element:
                                ProcessSqlNode(state, (XmlElement)childNode);
                                break;
                            case XmlNodeType.Text:
                            case XmlNodeType.Comment:
                                //ignore; valid text is in appropriate containers, displayable T-SQL comments are elements.
                                break;
                            default:
                                throw new Exception("Unexpected xml node type encountered!");
                        }
                    }
                    break;

                case SqlStructureConstants.ENAME_COMMENT_MULTILINE:
                    state.AddOutputContent("/*" + contentElement.InnerText + "*/", SqlHtmlConstants.CLASS_COMMENT);
                    break;
                case SqlStructureConstants.ENAME_COMMENT_SINGLELINE:
                    state.AddOutputContent("--" + contentElement.InnerText, SqlHtmlConstants.CLASS_COMMENT);
                    break;
                case SqlStructureConstants.ENAME_COMMENT_SINGLELINE_CSTYLE:
                    state.AddOutputContent("//" + contentElement.InnerText, SqlHtmlConstants.CLASS_COMMENT);
                    break;
                case SqlStructureConstants.ENAME_STRING:
                    state.AddOutputContent("'" + contentElement.InnerText.Replace("'", "''") + "'",
                        SqlHtmlConstants.CLASS_STRING);
                    break;
                case SqlStructureConstants.ENAME_NSTRING:
                    state.AddOutputContent("N'" + contentElement.InnerText.Replace("'", "''") + "'",
                        SqlHtmlConstants.CLASS_STRING);
                    break;
                case SqlStructureConstants.ENAME_QUOTED_STRING:
                    state.AddOutputContent("\"" + contentElement.InnerText.Replace("\"", "\"\"") + "\"");
                    break;
                case SqlStructureConstants.ENAME_BRACKET_QUOTED_NAME:
                    state.AddOutputContent("[" + contentElement.InnerText.Replace("]", "]]") + "]");
                    break;

                case SqlStructureConstants.ENAME_COMMA:
                case SqlStructureConstants.ENAME_PERIOD:
                case SqlStructureConstants.ENAME_SEMICOLON:
                case SqlStructureConstants.ENAME_ASTERISK:
                case SqlStructureConstants.ENAME_EQUALSSIGN:
                case SqlStructureConstants.ENAME_SCOPERESOLUTIONOPERATOR:
                case SqlStructureConstants.ENAME_AND_OPERATOR:
                case SqlStructureConstants.ENAME_OR_OPERATOR:
                case SqlStructureConstants.ENAME_ALPHAOPERATOR:
                case SqlStructureConstants.ENAME_OTHEROPERATOR:
                    state.AddOutputContent(contentElement.InnerText, SqlHtmlConstants.CLASS_OPERATOR);
                    break;

                case SqlStructureConstants.ENAME_FUNCTION_KEYWORD:
                    state.AddOutputContent(contentElement.InnerText, SqlHtmlConstants.CLASS_FUNCTION);
                    break;

                //case SqlStructureConstants.ENAME_PARAMETER:
                //    state.AddOutputContent("@" + contentElement.InnerText, "SQLParameter");
                //    break;

                case SqlStructureConstants.ENAME_OTHERKEYWORD:
                case SqlStructureConstants.ENAME_DATATYPE_KEYWORD:
                case SqlStructureConstants.ENAME_DDL_RETURNS:
                case SqlStructureConstants.ENAME_PSEUDONAME:
                    state.AddOutputContent(contentElement.InnerText, SqlHtmlConstants.CLASS_KEYWORD);
                    break;

                case SqlStructureConstants.ENAME_NUMBER_VALUE:
                case SqlStructureConstants.ENAME_MONETARY_VALUE:
                case SqlStructureConstants.ENAME_BINARY_VALUE:
                    state.AddOutputContent(contentElement.InnerText, "SQLNumber");
                    break;

                case SqlStructureConstants.ENAME_OTHERNODE:
                    string innerText = contentElement.InnerText;
                    if (innerText != null && VariableRegex.IsMatch(innerText))
                        state.AddOutputContent(innerText, "SQLParameter");
                    else
                        state.AddOutputContent(innerText);
                    break;
                case SqlStructureConstants.ENAME_WHITESPACE:
                case SqlStructureConstants.ENAME_LABEL:
                    state.AddOutputContent(contentElement.InnerText);
                    break;
                default:
                    throw new Exception("Unrecognized element in SQL Xml!");
            }

            if (contentElement.HasAttribute(SqlStructureConstants.ANAME_HASERROR) && contentElement.GetAttribute(SqlStructureConstants.ANAME_HASERROR) == "1")
                state.CloseClass();
        }


        public string FormatSQLTokens(ITokenList sqlTokenList)
        {
            StringBuilder outString = new StringBuilder();

            foreach (var entry in sqlTokenList)
            {
                switch (entry.Type)
                {
                    case SqlTokenType.MultiLineComment:
                        outString.Append("/*");
                        outString.Append(entry.Value);
                        outString.Append("*/");
                        break;
                    case SqlTokenType.SingleLineComment:
                        outString.Append("--");
                        outString.Append(entry.Value);
                        break;
                    case SqlTokenType.String:
                        outString.Append("'");
                        outString.Append(entry.Value.Replace("'", "''"));
                        outString.Append("'");
                        break;
                    case SqlTokenType.NationalString:
                        outString.Append("N'");
                        outString.Append(entry.Value.Replace("'", "''"));
                        outString.Append("'");
                        break;
                    case SqlTokenType.QuotedString:
                        outString.Append("\"");
                        outString.Append(entry.Value.Replace("\"", "\"\""));
                        outString.Append("\"");
                        break;
                    case SqlTokenType.BracketQuotedName:
                        outString.Append("[");
                        outString.Append(entry.Value.Replace("]", "]]"));
                        outString.Append("]");
                        break;

                    case SqlTokenType.OpenParens:
                    case SqlTokenType.CloseParens:
                    case SqlTokenType.Comma:
                    case SqlTokenType.Period:
                    case SqlTokenType.Semicolon:
                    case SqlTokenType.Colon:
                    case SqlTokenType.Asterisk:
                    case SqlTokenType.EqualsSign:
                    case SqlTokenType.OtherNode:
                    case SqlTokenType.WhiteSpace:
                    case SqlTokenType.OtherOperator:
                    case SqlTokenType.Number:
                    case SqlTokenType.BinaryValue:
                    case SqlTokenType.MonetaryValue:
                    case SqlTokenType.PseudoName:
                        outString.Append(entry.Value);
                        break;
                    default:
                        throw new Exception("Unrecognized Token Type in Token List!");
                }
            }

            return outString.ToString();
        }

    }
}
