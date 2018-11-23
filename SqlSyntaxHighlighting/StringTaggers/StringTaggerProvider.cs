using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using SqlSyntaxHighlighting.StringTaggers.CSharp;

namespace SqlSyntaxHighlighting.StringTaggers
{
	[Export(typeof(ITaggerProvider))]
	[ContentType("CSharp")]
	[TagType(typeof(StringTag))]
	internal class StringTaggerProvider : ITaggerProvider
	{
		[Import]
		internal IClassifierAggregatorService ClassifierAggregatorService { get; set; }

		[Import]
		internal IBufferTagAggregatorFactoryService TagAggregatorFactory { get; set; }

		public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
		{
			if (buffer == null)
				throw new ArgumentNullException("buffer");

            // Due to an issue with the built-in C# classifier, we avoid using it.
            if (buffer.ContentType.IsOfType("csharp"))
                return new CSharpStringTagger(buffer) as ITagger<T>;

            var classifierAggregator = ClassifierAggregatorService.GetClassifier(buffer);

			return new CommentTextTagger(buffer, classifierAggregator) as ITagger<T>;
		}
	}

	internal class CommentTextTagger : ITagger<StringTag>, IDisposable
	{
		readonly ITextBuffer buffer;
		readonly IClassifier classifier;

		public CommentTextTagger(ITextBuffer buffer, IClassifier classifier)
		{
			this.buffer = buffer;
			this.classifier = classifier;

			classifier.ClassificationChanged += ClassificationChanged;
		}

		public IEnumerable<ITagSpan<StringTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			if (classifier == null || spans == null || spans.Count == 0)
				yield break;

			foreach (var snapshotSpan in spans)
			{
				Debug.Assert(snapshotSpan.Snapshot.TextBuffer == buffer);
				foreach (ClassificationSpan classificationSpan in classifier.GetClassificationSpans(snapshotSpan))
				{
					string name = classificationSpan.ClassificationType.Classification.ToLowerInvariant();

					if (name.Contains("string")	&& name.Contains("xml doc tag") == false)
					{
						yield return new TagSpan<StringTag>(classificationSpan.Span, new StringTag());
					}
				}
			}
		}

		void ClassificationChanged(object sender, ClassificationChangedEventArgs e)
		{
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(e.ChangeSpan));
        }

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		public void Dispose()
		{
			if (classifier != null)
				classifier.ClassificationChanged -= ClassificationChanged;
		}
	}
}
