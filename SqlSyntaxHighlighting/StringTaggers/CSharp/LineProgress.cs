using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace SqlSyntaxHighlighting.StringTaggers.CSharp
{
    class LineProgress
    {
        private readonly ITextSnapshotLine snapshotLine;
        private readonly List<TagSpan<StringTag>> stringSpans;
		private readonly string lineText;
        private int stringStart = -1;
        private int interpolationStart = -1;
		private int linePosition;

        public State State { get; set; }
        private List<SnapshotSpan> interpolations;

        public LineProgress(ITextSnapshotLine line, State state, List<TagSpan<StringTag>> stringSpans)
        {
            snapshotLine = line;
            lineText = line.GetText();
            linePosition = 0;
            this.stringSpans = stringSpans;

            State = state;
        }

        public bool EndOfLine
        {
            get { return linePosition >= snapshotLine.Length; }
        }

        public char Char()
        {
            return lineText[linePosition];
        }

        public char NextChar()
        {
            return linePosition < snapshotLine.Length - 1 ?
                lineText[linePosition + 1] :
                (char)0;
        }

        public char NextNextChar()
        {
            return linePosition < snapshotLine.Length - 2 ?
                lineText[linePosition + 2] :
                (char)0;
        }

        public void Advance(int count = 1)
        {
            linePosition += count;
        }

        public void AdvanceToEndOfLine()
        {
            linePosition = snapshotLine.Length;
        }

        public void StartString(State typeOfString)
        {
            Debug.Assert(stringStart == -1, "Called StartString() twice without call to EndString()");
            stringStart = linePosition;
            State = typeOfString;
        }

        public void EndString(State newState = State.Default)
        {
            Debug.Assert(stringStart != -1, "Called EndString() without StartString()");
            if (stringSpans != null && linePosition > stringStart)
            {
                
                var span            = new SnapshotSpan(snapshotLine.Start + stringStart, linePosition - stringStart);
                bool isInterpolated = State == State.InterpolatedMultiLineString || State == State.InterpolatedString;
                var tag             = new StringTag { IsInterpolatedString = isInterpolated, Interpolations = interpolations };

                stringSpans.Add(new TagSpan<StringTag>(span, tag));

                interpolations = null;
                interpolationStart = -1;
            }

            stringStart = -1;
            State = newState;
        }

        public void StartStringInterpolation()
        {
            if (stringSpans == null) return;

            Debug.Assert(interpolationStart == -1, "Called StartStringInterpolation() without EndStringInterpolation()");
            Debug.Assert(State == State.InterpolatedMultiLineString || State == State.InterpolatedString, "Called StartStringInterpolation() without in interpolated string");
            interpolationStart = linePosition;
        }

        public void EndStringInterpolation()
        {
            if (stringSpans == null) return;

            Debug.Assert(interpolationStart != -1, "Called EndStringInterpolation() without StartStringInterpolation()");

            if (interpolations == null)
                interpolations = new List<SnapshotSpan>();

            interpolations.Add(new SnapshotSpan(snapshotLine.Start + interpolationStart, linePosition - interpolationStart));
            interpolationStart = -1;
            //var a = $"{$"{""}"}";
        }
    }
}