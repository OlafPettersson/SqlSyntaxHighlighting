using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public IEnumerable<KeyValuePair<string, string>> FormatSQLTree(XmlDocument sqlTreeDoc)
        {
            string rootElement = SqlXmlConstants.ENAME_SQL_ROOT;
            BaseFormatterState state = new BaseFormatterState();

            if (sqlTreeDoc.SelectSingleNode(string.Format("/{0}/@{1}[.=1]", rootElement, SqlXmlConstants.ANAME_ERRORFOUND)) != null)
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
            if (contentElement.GetAttribute(SqlXmlConstants.ANAME_HASERROR) == "1")
                state.OpenClass(SqlHtmlConstants.CLASS_ERRORHIGHLIGHT);

            switch (contentElement.Name)
            {
                case SqlXmlConstants.ENAME_DDLDETAIL_PARENS:
                case SqlXmlConstants.ENAME_DDL_PARENS:
                case SqlXmlConstants.ENAME_FUNCTION_PARENS:
                case SqlXmlConstants.ENAME_IN_PARENS:
                case SqlXmlConstants.ENAME_EXPRESSION_PARENS:
                case SqlXmlConstants.ENAME_SELECTIONTARGET_PARENS:
                    state.AddOutputContent("(");
                    ProcessSqlNodeList(state, contentElement.SelectNodes("*"));
                    state.AddOutputContent(")");
                    break;

                case SqlXmlConstants.ENAME_SQL_ROOT:
                case SqlXmlConstants.ENAME_SQL_STATEMENT:
                case SqlXmlConstants.ENAME_SQL_CLAUSE:
                case SqlXmlConstants.ENAME_BOOLEAN_EXPRESSION:
                case SqlXmlConstants.ENAME_DDL_PROCEDURAL_BLOCK:
                case SqlXmlConstants.ENAME_DDL_OTHER_BLOCK:
                case SqlXmlConstants.ENAME_DDL_DECLARE_BLOCK:
                case SqlXmlConstants.ENAME_CURSOR_DECLARATION:
                case SqlXmlConstants.ENAME_BEGIN_END_BLOCK:
                case SqlXmlConstants.ENAME_TRY_BLOCK:
                case SqlXmlConstants.ENAME_CATCH_BLOCK:
                case SqlXmlConstants.ENAME_CASE_STATEMENT:
                case SqlXmlConstants.ENAME_CASE_INPUT:
                case SqlXmlConstants.ENAME_CASE_WHEN:
                case SqlXmlConstants.ENAME_CASE_THEN:
                case SqlXmlConstants.ENAME_CASE_ELSE:
                case SqlXmlConstants.ENAME_IF_STATEMENT:
                case SqlXmlConstants.ENAME_ELSE_CLAUSE:
                case SqlXmlConstants.ENAME_WHILE_LOOP:
                case SqlXmlConstants.ENAME_DDL_AS_BLOCK:
                case SqlXmlConstants.ENAME_BETWEEN_CONDITION:
                case SqlXmlConstants.ENAME_BETWEEN_LOWERBOUND:
                case SqlXmlConstants.ENAME_BETWEEN_UPPERBOUND:
                case SqlXmlConstants.ENAME_CTE_WITH_CLAUSE:
                case SqlXmlConstants.ENAME_CTE_ALIAS:
                case SqlXmlConstants.ENAME_CTE_AS_BLOCK:
                case SqlXmlConstants.ENAME_CURSOR_FOR_BLOCK:
                case SqlXmlConstants.ENAME_CURSOR_FOR_OPTIONS:
                case SqlXmlConstants.ENAME_TRIGGER_CONDITION:
                case SqlXmlConstants.ENAME_COMPOUNDKEYWORD:
                case SqlXmlConstants.ENAME_BEGIN_TRANSACTION:
                case SqlXmlConstants.ENAME_ROLLBACK_TRANSACTION:
                case SqlXmlConstants.ENAME_SAVE_TRANSACTION:
                case SqlXmlConstants.ENAME_COMMIT_TRANSACTION:
                case SqlXmlConstants.ENAME_BATCH_SEPARATOR:
                case SqlXmlConstants.ENAME_SET_OPERATOR_CLAUSE:
                case SqlXmlConstants.ENAME_CONTAINER_OPEN:
                case SqlXmlConstants.ENAME_CONTAINER_MULTISTATEMENT:
                case SqlXmlConstants.ENAME_CONTAINER_SINGLESTATEMENT:
                case SqlXmlConstants.ENAME_CONTAINER_GENERALCONTENT:
                case SqlXmlConstants.ENAME_CONTAINER_CLOSE:
                case SqlXmlConstants.ENAME_SELECTIONTARGET:
                case SqlXmlConstants.ENAME_PERMISSIONS_BLOCK:
                case SqlXmlConstants.ENAME_PERMISSIONS_DETAIL:
                case SqlXmlConstants.ENAME_PERMISSIONS_TARGET:
                case SqlXmlConstants.ENAME_PERMISSIONS_RECIPIENT:
                case SqlXmlConstants.ENAME_DDL_WITH_CLAUSE:
                case SqlXmlConstants.ENAME_MERGE_CLAUSE:
                case SqlXmlConstants.ENAME_MERGE_TARGET:
                case SqlXmlConstants.ENAME_MERGE_USING:
                case SqlXmlConstants.ENAME_MERGE_CONDITION:
                case SqlXmlConstants.ENAME_MERGE_WHEN:
                case SqlXmlConstants.ENAME_MERGE_THEN:
                case SqlXmlConstants.ENAME_MERGE_ACTION:
                case SqlXmlConstants.ENAME_JOIN_ON_SECTION:
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

                case SqlXmlConstants.ENAME_COMMENT_MULTILINE:
                    state.AddOutputContent("/*" + contentElement.InnerText + "*/", SqlHtmlConstants.CLASS_COMMENT);
                    break;
                case SqlXmlConstants.ENAME_COMMENT_SINGLELINE:
                    state.AddOutputContent("--" + contentElement.InnerText, SqlHtmlConstants.CLASS_COMMENT);
                    break;
                case SqlXmlConstants.ENAME_COMMENT_SINGLELINE_CSTYLE:
                    state.AddOutputContent("//" + contentElement.InnerText, SqlHtmlConstants.CLASS_COMMENT);
                    break;
                case SqlXmlConstants.ENAME_STRING:
                    state.AddOutputContent("'" + contentElement.InnerText.Replace("'", "''") + "'",
                        SqlHtmlConstants.CLASS_STRING);
                    break;
                case SqlXmlConstants.ENAME_NSTRING:
                    state.AddOutputContent("N'" + contentElement.InnerText.Replace("'", "''") + "'",
                        SqlHtmlConstants.CLASS_STRING);
                    break;
                case SqlXmlConstants.ENAME_QUOTED_STRING:
                    state.AddOutputContent("\"" + contentElement.InnerText.Replace("\"", "\"\"") + "\"");
                    break;
                case SqlXmlConstants.ENAME_BRACKET_QUOTED_NAME:
                    state.AddOutputContent("[" + contentElement.InnerText.Replace("]", "]]") + "]");
                    break;

                case SqlXmlConstants.ENAME_COMMA:
                case SqlXmlConstants.ENAME_PERIOD:
                case SqlXmlConstants.ENAME_SEMICOLON:
                case SqlXmlConstants.ENAME_ASTERISK:
                case SqlXmlConstants.ENAME_EQUALSSIGN:
                case SqlXmlConstants.ENAME_SCOPERESOLUTIONOPERATOR:
                case SqlXmlConstants.ENAME_AND_OPERATOR:
                case SqlXmlConstants.ENAME_OR_OPERATOR:
                case SqlXmlConstants.ENAME_ALPHAOPERATOR:
                case SqlXmlConstants.ENAME_OTHEROPERATOR:
                    state.AddOutputContent(contentElement.InnerText, SqlHtmlConstants.CLASS_OPERATOR);
                    break;

                case SqlXmlConstants.ENAME_FUNCTION_KEYWORD:
                    state.AddOutputContent(contentElement.InnerText, SqlHtmlConstants.CLASS_FUNCTION);
                    break;

                case SqlXmlConstants.ENAME_PARAMETER:
                    state.AddOutputContent("@" + contentElement.InnerText, "SQLParameter");
                    break;

                case SqlXmlConstants.ENAME_OTHERKEYWORD:
                case SqlXmlConstants.ENAME_DATATYPE_KEYWORD:
                case SqlXmlConstants.ENAME_DDL_RETURNS:
                case SqlXmlConstants.ENAME_PSEUDONAME:
                    state.AddOutputContent(contentElement.InnerText, SqlHtmlConstants.CLASS_KEYWORD);
                    break;

                case SqlXmlConstants.ENAME_NUMBER_VALUE:
                case SqlXmlConstants.ENAME_MONETARY_VALUE:
                case SqlXmlConstants.ENAME_BINARY_VALUE:
                    state.AddOutputContent(contentElement.InnerText, "SQLNumber");
                    break;

                case SqlXmlConstants.ENAME_OTHERNODE:
                case SqlXmlConstants.ENAME_WHITESPACE:
                case SqlXmlConstants.ENAME_LABEL:
                    state.AddOutputContent(contentElement.InnerText);
                    break;
                default:
                    throw new Exception("Unrecognized element in SQL Xml!");
            }

            if (contentElement.HasAttribute(SqlXmlConstants.ANAME_HASERROR) && contentElement.GetAttribute(SqlXmlConstants.ANAME_HASERROR) == "1")
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
