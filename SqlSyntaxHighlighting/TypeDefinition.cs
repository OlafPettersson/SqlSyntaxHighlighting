using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace SqlSyntaxHighlighting
{
    public static class TypeDefinition
    {
        [Export(typeof(ClassificationTypeDefinition))]
        [Name("sql-keyword")]
        internal static ClassificationTypeDefinition SqlKeywordType;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = "sql-keyword")]
        [Name("SQL Syntax Highlighting - Keyword")]
        [DisplayName("SQL Syntax Highlighting - Keyword")]
        [UserVisible(true)]
        [Order(Before = Priority.High, After = Priority.High)]
        internal sealed class SqlKeywordFormat : ClassificationFormatDefinition
        {
            public SqlKeywordFormat()
            {
                this.ForegroundColor = Color.FromRgb(86, 156, 214);
            }
        }

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("sql-function")]
        internal static ClassificationTypeDefinition SqlFunctionType;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = "sql-function")]
        [Name("SQL Syntax Highlighting - Function")]
        [DisplayName("SQL Syntax Highlighting - Function")]
        [UserVisible(true)]
        [Order(Before = Priority.High, After = Priority.High)]
        internal sealed class SqlFunctionFormat : ClassificationFormatDefinition
        {
            public SqlFunctionFormat()
            {
                this.ForegroundColor = Color.FromRgb(197, 99, 189);
            }
        }

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("sql-variable")]
        internal static ClassificationTypeDefinition SqlVariableType;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = "sql-variable")]
        [Name("SQL Syntax Highlighting - Variable")]
        [DisplayName("SQL Syntax Highlighting - Variable")]
        [UserVisible(true)]
        [Order(Before = Priority.High, After = Priority.High)]
        internal sealed class SqlVariableFormat : ClassificationFormatDefinition
        {
            public SqlVariableFormat()
            {
                this.ForegroundColor = Color.FromRgb(86, 156, 214);
            }
        }

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("sql-operator")]
        internal static ClassificationTypeDefinition SqlOperatorType;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = "sql-operator")]
        [Name("SQL Syntax Highlighting - Operator")]
        [DisplayName("SQL Syntax Highlighting - Operator")]
        [UserVisible(true)]
        [Order(Before = Priority.High, After = Priority.High)]
        internal sealed class SqlOperatorFormat : ClassificationFormatDefinition
        {
            public SqlOperatorFormat()
            {
                this.ForegroundColor = Color.FromRgb(180, 180, 180);
            }
        }

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("sql-string")]
        internal static ClassificationTypeDefinition SqlStringType;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = "sql-string")]
        [Name("SQL Syntax Highlighting - String")]
        [DisplayName("SQL Syntax Highlighting - String")]
        [UserVisible(true)]
        [Order(Before = Priority.High, After = Priority.High)]
        internal sealed class SqlStringFormat : ClassificationFormatDefinition
        {
            public SqlStringFormat()
            {
                this.ForegroundColor = Color.FromRgb(203, 65, 65);
            }
        }

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("sql-number")]
        internal static ClassificationTypeDefinition SqlNumberType;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = "sql-number")]
        [Name("SQL Syntax Highlighting - Number")]
        [DisplayName("SQL Syntax Highlighting - Number")]
        [UserVisible(true)]
        [Order(Before = Priority.High, After = Priority.High)]
        internal sealed class SqlNumberFormat : ClassificationFormatDefinition
        {
            public SqlNumberFormat()
            {
                this.ForegroundColor = Color.FromRgb(181, 206, 168);
            }
        }

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("sql-parameter")]
        internal static ClassificationTypeDefinition SqlParameterType;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = "sql-parameter")]
        [Name("SQL Syntax Highlighting - Parameter")]
        [DisplayName("SQL Syntax Highlighting - Parameter")]
        [UserVisible(true)]
        [Order(Before = Priority.High, After = Priority.High)]
        internal sealed class SqlParameterFormat : ClassificationFormatDefinition
        {
            public SqlParameterFormat()
            {
                this.ForegroundColor = Colors.Firebrick;
                this.IsBold = true;
            }
        }

        [Export(typeof(ClassificationTypeDefinition))]
        [Name("sql-comment")]
        internal static ClassificationTypeDefinition SqlCommentType;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = "sql-comment")]
        [Name("SQL Syntax Highlighting - Comment")]
        [DisplayName("SQL Syntax Highlighting - Comment")]
        [UserVisible(true)]
        [Order(Before = Priority.High, After = Priority.High)]
        internal sealed class SqlCommentFormat : ClassificationFormatDefinition
        {
            public SqlCommentFormat()
            {
                this.ForegroundColor = Color.FromRgb(87, 166, 74);
            }
        }
    }
}