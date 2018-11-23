using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Media.TextFormatting;
using System.Xml;
using ConsoleApplication1;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using PoorMansTSqlFormatterLib;
using PoorMansTSqlFormatterLib.Formatters;
using PoorMansTSqlFormatterLib.Interfaces;
using PoorMansTSqlFormatterLib.Parsers;
using PoorMansTSqlFormatterLib.Tokenizers;
using SqlSyntaxHighlighting.StringTaggers;

namespace SqlSyntaxHighlighting
{
    class SqlClassifier : IClassifier
    {
        private readonly IClassificationType keywordType;
        private readonly IClassificationType functionType;
        readonly ITagAggregator<StringTag> tagger;

        private CustomFormatter formatter;
        private TSqlStandardTokenizer tokenizer;
        private TSqlStandardParser parser;
        private IClassificationType operatorType;
        private IClassificationType stringType;
        private IClassificationType numberType;
        private IClassificationType parameterType;
        private IClassificationType commentType;

        internal SqlClassifier(ITagAggregator<StringTag> tagger, IClassificationTypeRegistryService classificationRegistry)
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
            List<ClassificationSpan> classifiedSpans = new List<ClassificationSpan>();

            // Note: best would be to avoid recalulating the output everytime on multiline SQL.
            // I am not sure how to achive this while avoiding chaching potentially huge documents.

            var tagSpans = tagger.GetTags(span);
            foreach (var tagSpan in tagSpans)
            {
                var snapshotSpan = tagSpan.Span.GetSpans(span.Snapshot).First();

                var text = snapshotSpan.GetText();

                if (!IsKnownStartOfSql(text))
                    break;

                // TODO: think about what is the best way to handle '{ }', both in normal 
                //       as well as in interpolated strings. most futureproof might be to 
                //       actually use the roslyn code analyzer.

                List<Span> stringInterpolations = new List<Span>();
                int stringInterpolationIndex = 0;

                if (tagSpan.Tag.Interpolations != null)
                {
                    // replace interpolations with "xxxx", to avoid confusing PoorMansTSqlFormatter

                    StringBuilder b = new StringBuilder(text);
                    foreach (var interpolation in tagSpan.Tag.Interpolations)
                    {
                        int relStart  = interpolation.Start.Position - snapshotSpan.Start.Position;
                        int relLength = interpolation.Length;
                        for (int i = 0; i < relLength; ++i)
                            b[i + relStart] = 'x';

                        stringInterpolations.Add(interpolation.Span);
                    }

                    text = b.ToString();
                    stringInterpolations.OrderBy(i => i.Start).ThenBy(i => i.Length).ToList();
                }

                var index = 0;

                var tokens = tokenizer.TokenizeSQL(text);
                var doc    = parser.ParseSQL(tokens).ToXmlDoc();

                // no need to be too strict
                //if (doc.DocumentElement.HasAttribute("errorFound") && doc.DocumentElement.GetAttribute("errorFound").Equals("1", StringComparison.InvariantCultureIgnoreCase))
                //    continue;
                if (doc.DocumentElement == null)
                    continue;

                //$"SELECT { --index } FROM x";

                var sqlWithType = formatter.FormatSQLTree(doc);

                foreach (var pair in sqlWithType)
                {
                    int length          = pair.Value.Length;
                    SnapshotPoint start = snapshotSpan.Start + index;

                    index += length;

                    string classification = pair.Key;
                    if (classification == null)
                        continue;

                    Span cutOutinterpolation = default(Span);
                    // cut out string interpolations, if any.
                    while (stringInterpolationIndex < stringInterpolations.Count)
                    {
                        Span cutOut = stringInterpolations[stringInterpolationIndex];
                        if (cutOut.Start > start.Position + length)
                            break;

                        if (cutOut.Start + cutOut.Length < start.Position)
                        {
                            ++stringInterpolationIndex;
                            continue;
                        }

                        // there is some overlap.
                        cutOutinterpolation = cutOut;
                        break;
                    }

                    if(cutOutinterpolation.IsEmpty)
                    {
                        AddClassification(classifiedSpans, classification, new SnapshotSpan(start, length));
                    }
                    else
                    {
                        if (cutOutinterpolation.Start > start.Position)
                        {
                            AddClassification(classifiedSpans, classification, new SnapshotSpan(start, cutOutinterpolation.Start - start.Position));
                        }
                        if (cutOutinterpolation.Start + cutOutinterpolation.Length < start.Position + length)
                        {
                            int keepStart = cutOutinterpolation.Start + cutOutinterpolation.Length;
                            int keepLength = start.Position + length - keepStart;
                            AddClassification(classifiedSpans, classification, new SnapshotSpan(new SnapshotPoint(start.Snapshot, keepStart), keepLength));
                        }
                    }
                }
            }

            return classifiedSpans;
        }

        private void AddClassification(List<ClassificationSpan> classifiedSpans, string classification, SnapshotSpan classifiedSpan)
        {
            if (classification.Equals("SQLOperator", StringComparison.InvariantCultureIgnoreCase))
                classifiedSpans.Add(new ClassificationSpan(classifiedSpan, operatorType));

            if (classification.Equals("SQLKeyword", StringComparison.InvariantCultureIgnoreCase))
                classifiedSpans.Add(new ClassificationSpan(classifiedSpan, keywordType));

            if (classification.Equals("SQLString", StringComparison.InvariantCultureIgnoreCase))
                classifiedSpans.Add(new ClassificationSpan(classifiedSpan, stringType));

            if (classification.Equals("SQLFunction", StringComparison.InvariantCultureIgnoreCase))
                classifiedSpans.Add(new ClassificationSpan(classifiedSpan, functionType));

            if (classification.Equals("SQLNumber", StringComparison.InvariantCultureIgnoreCase))
                classifiedSpans.Add(new ClassificationSpan(classifiedSpan, numberType));

            if (classification.Equals("SQLParameter", StringComparison.InvariantCultureIgnoreCase))
                classifiedSpans.Add(new ClassificationSpan(classifiedSpan, parameterType));

            if (classification.Equals("SQLComment", StringComparison.InvariantCultureIgnoreCase))
                classifiedSpans.Add(new ClassificationSpan(classifiedSpan, commentType));
        }

        // we include all statement start keywords, and some typical continuation keywords. only allow upper casing, to avoid too many false positives.
        private static readonly Regex KnownSqlStarts = new Regex(@"^(?:\s|[\r\n])*(MERGE|SELECT|UPDATE|INSERT|DELETE|WITH|TRUNCATE|DROP|CREATE|ALTER|GRANT|REVOKE|EXECUTE|EXEC|COMMIT|BEGIN|ROLLBACK|START|WHERE|AND|ORDER BY|SAVE|USING|SET|ON|CASE|WHEN|THEN|OUTPUT|END|OPTION|GO|UNION|INTERSECT|EXCEPT|WHILE|IF|ELSE|BULK INSERT|VALUES|FROM|WITH|HAVING|MERGE)\b"); 

        private bool IsKnownStartOfSql(string text)
        {
            return KnownSqlStarts.IsMatch(text);
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged
        {
            add { }
            remove { }
        }
    }
}