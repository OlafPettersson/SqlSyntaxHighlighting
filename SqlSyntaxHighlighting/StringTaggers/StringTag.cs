using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System.Collections.Generic;

namespace SqlSyntaxHighlighting.StringTaggers
{
	class StringTag : ITag
	{
        public bool IsInterpolatedString { get; set; }

        public IList<SnapshotSpan> Interpolations { get; set; }
	}
}
