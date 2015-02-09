using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Media.TextFormatting;
using System.Xml;
using ConsoleApplication1;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using PoorMansTSqlFormatterLib;
using PoorMansTSqlFormatterLib.Formatters;
using PoorMansTSqlFormatterLib.Interfaces;
using PoorMansTSqlFormatterLib.Parsers;
using PoorMansTSqlFormatterLib.Tokenizers;
using SqlSyntaxHighlighting.NaturalTextTaggers;

namespace SqlSyntaxHighlighting
{
    class SqlClassifier : IClassifier
    {
        private readonly IClassificationType keywordType;
        private readonly IClassificationType functionType;
        readonly ITagAggregator<NaturalTextTag> tagger;

        private CustomFormatter formatter;
        private TSqlStandardTokenizer tokenizer;
        private TSqlStandardParser parser;
        private IClassificationType operatorType;
        private IClassificationType stringType;
        private IClassificationType numberType;
        private IClassificationType parameterType;
        private IClassificationType commentType;

        internal SqlClassifier(ITagAggregator<NaturalTextTag> tagger, IClassificationTypeRegistryService classificationRegistry)
        {
            this.tagger = tagger;

            keywordType = classificationRegistry.GetClassificationType("sql-keyword");
            operatorType = classificationRegistry.GetClassificationType("sql-operator");
            stringType = classificationRegistry.GetClassificationType("sql-string");
            functionType = classificationRegistry.GetClassificationType("sql-function");
            numberType = classificationRegistry.GetClassificationType("sql-number");
            parameterType = classificationRegistry.GetClassificationType("sql-parameter");
            commentType = classificationRegistry.GetClassificationType("sql-comment");

            formatter = new CustomFormatter();
            tokenizer = new TSqlStandardTokenizer();
            parser = new TSqlStandardParser();
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            IList<ClassificationSpan> classifiedSpans = new List<ClassificationSpan>();

            foreach (var tagSpan in tagger.GetTags(span))
            {
                var snapshot = tagSpan.Span.GetSpans(span.Snapshot).First();

                var text = snapshot.GetText();
                var index = 0;

                var tokens = tokenizer.TokenizeSQL(text);
                var doc = parser.ParseSQL(tokens);
                var sql = formatter.FormatSQLTree(doc);

                if (doc.DocumentElement == null || (doc.DocumentElement.HasAttribute("errorFound") && doc.DocumentElement.GetAttribute("errorFound").Equals("1", StringComparison.InvariantCultureIgnoreCase)))
                    return classifiedSpans;

                foreach (var pair in sql)
                {
                    var length = pair.Value.Length;

                    if (pair.Key != null)
                    {
                        if (pair.Key.Equals("SQLOperator", StringComparison.InvariantCultureIgnoreCase))
                            classifiedSpans.Add(new ClassificationSpan(new SnapshotSpan(snapshot.Start + index, length), operatorType));

                        if (pair.Key.Equals("SQLKeyword", StringComparison.InvariantCultureIgnoreCase))
                            classifiedSpans.Add(new ClassificationSpan(new SnapshotSpan(snapshot.Start + index, length), keywordType));

                        if (pair.Key.Equals("SQLString", StringComparison.InvariantCultureIgnoreCase))
                            classifiedSpans.Add(new ClassificationSpan(new SnapshotSpan(snapshot.Start + index, length), stringType));

                        if (pair.Key.Equals("SQLFunction", StringComparison.InvariantCultureIgnoreCase))
                            classifiedSpans.Add(new ClassificationSpan(new SnapshotSpan(snapshot.Start + index, length), functionType));

                        if (pair.Key.Equals("SQLNumber", StringComparison.InvariantCultureIgnoreCase))
                            classifiedSpans.Add(new ClassificationSpan(new SnapshotSpan(snapshot.Start + index, length), numberType));

                        if (pair.Key.Equals("SQLParameter", StringComparison.InvariantCultureIgnoreCase))
                            classifiedSpans.Add(new ClassificationSpan(new SnapshotSpan(snapshot.Start + index, length), parameterType));

                        if (pair.Key.Equals("SQLComment", StringComparison.InvariantCultureIgnoreCase))
                            classifiedSpans.Add(new ClassificationSpan(new SnapshotSpan(snapshot.Start + index, length), commentType));
                    }

                    index += length;
                }
            }

            return classifiedSpans;
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged
        {
            add { }
            remove { }
        }
    }
}