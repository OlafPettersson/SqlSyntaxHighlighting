using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace SqlSyntaxHighlighting.StringTaggers.CSharp
{
	/// <summary>
	/// Due to issues with the built-in C# classifier, we write our own StringTagger that looks for 
	/// comments (single, multi-line, and doc comment) and strings (single and multi-line). Strings get 
    /// taged with StringTag.
    /// 
    /// Note: At the moment, we do not detect multiline string /**/
	/// </summary>
	internal class CSharpStringTagger : ITagger<StringTag>, IDisposable
	{
		readonly ITextBuffer buffer;
		ITextSnapshot lineCacheSnapshot;
		readonly List<State> lineCache;

		public CSharpStringTagger(ITextBuffer buffer)
		{
			this.buffer = buffer;

			// Populate our cache initially.
			ITextSnapshot snapshot = this.buffer.CurrentSnapshot;
			lineCache = new List<State>(snapshot.LineCount);
			lineCache.AddRange(Enumerable.Repeat(State.Default, snapshot.LineCount));

			RescanLines(snapshot, 0, snapshot.LineCount - 1);
			lineCacheSnapshot = snapshot;

			// Listen for text changes so we can stay up-to-date
			this.buffer.Changed += OnTextBufferChanged;
		}

		public void Dispose()
		{
			buffer.Changed -= OnTextBufferChanged;
		}

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;


        public IEnumerable<ITagSpan<StringTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            ;
            foreach (SnapshotSpan span in spans)
            {
                // If we're called on the non-current snapshot, return nothing
                if (span.Snapshot != lineCacheSnapshot)
                    yield break;

                // find beginning of multi-line strings, if any.
                SnapshotPoint lineStart = span.Start;
                State prevState = State.Default;

                while (lineStart.Position > 0)
                {
                    ITextSnapshotLine line = lineStart.GetContainingLine();

                    if (line.LineNumber <= 0 || !IsMultilineString(lineCache[line.LineNumber - 1]))
                    {
                        prevState = lineCache[line.LineNumber - 1];
                        break;
                    }
                    lineStart = line.Start.Subtract(1);
                }

                // return all string text tags; including full multiline strings.

                TagSpan<StringTag> currentMultilineSpan = null;

                while (lineStart < span.End || (lineStart.Position < lineCacheSnapshot.Length && IsMultilineString(prevState)))
                {
                    ITextSnapshotLine line = lineStart.GetContainingLine();

                    var stringSpans = new List<TagSpan<StringTag>>();
                    State newState = ScanLine(prevState, line, stringSpans);

                    if (stringSpans.Count > 0)
                    {
                        int naturalTextSpansFirst = 0;
                        int naturalTextSpansLast = stringSpans.Count - 1;

                        // extend multiline span if required.
                        if (IsMultilineString(prevState))
                        {
                            if (currentMultilineSpan == null) // first line was empty.
                            {
                                currentMultilineSpan = stringSpans[naturalTextSpansFirst];
                            }
                            else
                            {
                                currentMultilineSpan = CombineSpans(currentMultilineSpan, stringSpans[naturalTextSpansFirst]);
                            }
                            ++naturalTextSpansFirst;
                        }

                        // emit last multiline span if we end multiline on this line, or there are more spans on the same line.
                        if(IsMultilineString(prevState) && (!IsMultilineString(newState) || naturalTextSpansFirst <= naturalTextSpansLast))
                        {
                            if (currentMultilineSpan.Span.IntersectsWith(span))
                                yield return currentMultilineSpan;
                        }

                        // start new multiline when required.
                        if (IsMultilineString(newState) && naturalTextSpansFirst <= naturalTextSpansLast)
                        {
                            currentMultilineSpan = stringSpans[naturalTextSpansLast];
                            --naturalTextSpansLast;
                        }

                        // emit all normal spans on this line
                        for(int i = naturalTextSpansFirst; i <= naturalTextSpansLast; ++i)
                        {
                            if (stringSpans[i].Span.IntersectsWith(span))
                                yield return stringSpans[i];
                        }
                    }

                    // Advance to next line
                    lineStart = line.EndIncludingLineBreak;
                    prevState = newState;
                }
            }
        }

        private static TagSpan<StringTag> CombineSpans(TagSpan<StringTag> oneSpan, TagSpan<StringTag> addSpan)
        {
            var tag = new StringTag
            {
                IsInterpolatedString = oneSpan.Tag.IsInterpolatedString,
                Interpolations =       oneSpan.Tag.Interpolations == null ? addSpan.Tag.Interpolations
                                     : addSpan.Tag.Interpolations == null ? oneSpan.Tag.Interpolations
                                     : oneSpan.Tag.Interpolations.Concat(addSpan.Tag.Interpolations).ToList()
            };
            return new TagSpan<StringTag>(new SnapshotSpan(oneSpan.Span.Start, addSpan.Span.End), tag);
        }

        private bool IsMultilineString(State state)
        {
            return state == State.InterpolatedMultiLineString || state == State.MultiLineString;
        }

        private void OnTextBufferChanged(object sender, TextContentChangedEventArgs e)
		{
			ITextSnapshot snapshot = e.After;

			// First update _lineCache so its size matches snapshot.LineCount
			foreach (ITextChange change in e.Changes)
			{
				if (change.LineCountDelta > 0)
				{
					int line = snapshot.GetLineFromPosition(change.NewPosition).LineNumber;
					lineCache.InsertRange(line, Enumerable.Repeat(State.Default, change.LineCountDelta));
				}
				else if (change.LineCountDelta < 0)
				{
					int line = snapshot.GetLineFromPosition(change.NewPosition).LineNumber;
					lineCache.RemoveRange(line, -change.LineCountDelta);
				}
			}

			// Now that _lineCache is the appropriate size we can safely start rescanning.
			// If we hadn't updated _lineCache, then rescanning could walk off the edge.
			List<SnapshotSpan> changedSpans = (from change in e.Changes
			                                   let startLine = snapshot.GetLineFromPosition(change.NewPosition)
			                                   let endLine = snapshot.GetLineFromPosition(change.NewPosition)
			                                   let lastUpdatedLine = RescanLines(snapshot, startLine.LineNumber, endLine.LineNumber)
			                                   select new SnapshotSpan(startLine.Start, snapshot.GetLineFromLineNumber(lastUpdatedLine).End)).ToList();

			lineCacheSnapshot = snapshot;

			var tagsChanged = TagsChanged;
			if (tagsChanged != null)
			{
				foreach (SnapshotSpan span in changedSpans)
				{
					tagsChanged(this, new SnapshotSpanEventArgs(span));
				}
			}
		}

		// Returns last line updated (will be greater than or equal to lastDirtyLine)
		private int RescanLines(ITextSnapshot snapshot, int startLine, int lastDirtyLine)
		{
			int currentLine = startLine;
			bool updatedStateForCurrentLine = true;
            State state = currentLine <= 0 ? State.Default : lineCache[currentLine - 1];

			// Go until we have covered all of the dirty lines and we get to a line where our
			// new state matches the old state
			while (currentLine < lastDirtyLine || (updatedStateForCurrentLine && currentLine < snapshot.LineCount))
			{
				ITextSnapshotLine line = snapshot.GetLineFromLineNumber(currentLine);
				state = ScanLine(state, line);

				if (currentLine < snapshot.LineCount)
				{
					updatedStateForCurrentLine = (state != lineCache[currentLine]);
					lineCache[currentLine] = state;
				}

				// Advance to next line
				currentLine++;
			}

			// Last line updated
			return currentLine - 1;
		}

		private State ScanLine(State state, ITextSnapshotLine line, List<TagSpan<StringTag>> naturalTextSpans = null)
		{
			LineProgress p = new LineProgress(line, state, naturalTextSpans);

			while (!p.EndOfLine)
			{
                if (p.State == State.Default)
                    ScanDefault(p);
                else if (p.State == State.MultiLineString || p.State == State.InterpolatedMultiLineString)
                {
                    p.StartString(p.State);
                    ScanMultiLineString(p, isInterpolated: p.State == State.InterpolatedMultiLineString);
                }
                else if (p.State == State.MultilineComment)
                    ScanMultiLineComment(p);
                else
                    Debug.Fail("Invalid state at beginning of line.");
			}

			// End Of Line state must be one of these.
			Debug.Assert(p.State == State.Default 
                      || p.State == State.MultiLineString 
                      || p.State == State.InterpolatedMultiLineString 
                      || p.State == State.MultilineComment);

			return p.State;
		}

		private void ScanDefault(LineProgress p)
		{
            while (!p.EndOfLine)
            {
                if (p.Char() == '$' && p.NextChar() == '@' && p.NextNextChar() == '"') // interpolated multiline string
                {
                    p.Advance(3);
                    p.StartString(State.InterpolatedMultiLineString);
                    ScanMultiLineString(p, true);
                }
                else if (p.Char() == '$' && p.NextChar() == '"') // interpolated string
                {
                    p.Advance(2);
                    p.StartString(State.InterpolatedString);
                    ScanString(p, true);
                }
                else
                if (p.Char() == '@' && p.NextChar() == '"') // multi-line string
                {
                    p.Advance(2);
                    p.StartString(State.MultiLineString);
                    ScanMultiLineString(p, false);
                }
                else if (p.Char() == '"') // single-line string
                {
                    p.Advance();  
                    p.StartString(State.String);
                    ScanString(p, false);
                }
                else if (p.Char() == '\'') // character
                {
                    p.Advance();
                    ScanCharacter(p);
                }
                else if (p.Char() == '/' && p.NextChar() == '*') // multiline comment
                {
                    p.Advance(2);
                    p.State = State.MultilineComment;
                    ScanMultiLineComment(p);
                }
                else if (p.Char() == '/' && p.NextChar() == '/') // single-line comment
                {
                    p.AdvanceToEndOfLine();
                }
                else
                {
                    p.Advance();
                }
            }
		}

		private void ScanString(LineProgress p, bool isInterpolated, bool skipOnly = false)
		{
			while (!p.EndOfLine)
			{
				if (p.Char() == '\\') // escaped character. Skip over it.
				{
					p.Advance(2);
				}
                else if (isInterpolated && p.Char() == '{' && p.NextChar() == '{') // escaped interpolation 
                {
                    p.Advance(2);
                }
                else if (isInterpolated && p.Char() == '{' && p.NextChar() != '{') // interpolation.
                {
                    if (!skipOnly)
                        p.StartStringInterpolation();
                    p.Advance();
                    ScanInterpolation(p, skipOnly);
                }
                else if (p.Char() == '"') // end of string.
				{
                    if (!skipOnly)
                        p.EndString();
                    p.Advance();
					return;
				}
                else
				{
					p.Advance();
				}
			}

            // End of line.  String wasn't closed.  Oh well.  Revert to Default state.
            if (!skipOnly)
            {
                p.EndString();
            }
		}

        private void ScanMultiLineString(LineProgress p, bool isInterpolated, bool skipOnly = false)
		{
			while (!p.EndOfLine)
            {
				if (p.Char() == '"' && p.NextChar() == '"') // "" is allowed within multiline string
				{
					p.Advance(2);
				}
                else if (isInterpolated && p.Char() == '{' && p.NextChar() == '{') // escaped interpolation 
                {
                    p.Advance(2);
                }
                else if (isInterpolated && p.Char() == '{' && p.NextChar() != '{') // interpolation.
                {
                    if (!skipOnly)
                        p.StartStringInterpolation();
                    p.Advance();
                    ScanInterpolation(p, skipOnly);
                }
                else if (p.Char() == '"') // end of multi-line string
				{
                    if(!skipOnly)
                        p.EndString();

                    p.Advance();
                    return;
				}
				else
                {
					p.Advance();
				}
			}

            if (!skipOnly)
            {
                // End of line. Emit as string, but remain in MultiLineString state
                p.EndString(p.State);
            }
		}

    

        private void ScanCharacter(LineProgress p, bool skipOnly = false)
        {
            while (!p.EndOfLine)
            {
                if (p.Char() == '\\') // escaped character. Skip over it.
                {
                    p.Advance(2);
                }
                else if (p.Char() == '\'') // end of char
                {
                    p.Advance();
                    if(!skipOnly)
                        p.State = State.Default;
                    return;
                }
                else
                {
                    p.Advance();
                }
            }

            if (!skipOnly)
            {
                // End of line.  Char wasn't closed.  Oh well.  Revert to Default state.
                p.State = State.Default;
            }
        }

        private void ScanInterpolation(LineProgress p, bool skipOnly)
        {
            while (!p.EndOfLine)
            {
                if (p.Char() == '$' && p.NextChar() == '@' && p.NextNextChar() == '"') // interpolated multiline string
                {
                    p.Advance(3);
                    ScanMultiLineString(p, true, true);
                }
                else if (p.Char() == '$' && p.NextChar() == '"') // interpolated string
                {
                    p.Advance(2);
                    ScanString(p, true, true);
                }
                else
                if (p.Char() == '@' && p.NextChar() == '"') // multi-line string
                {
                    p.Advance(2);
                    ScanMultiLineString(p, false, true);
                }
                else if (p.Char() == '"') // single-line string
                {
                    p.Advance();
                    ScanString(p, false, true);
                }
                else if (p.Char() == '\'') // character
                {
                    p.Advance();
                    ScanCharacter(p, true);
                }
                else if (p.Char() == '/' && p.NextChar() == '*') // multiline comment
                {
                    p.Advance();
                    ScanMultiLineComment(p, true); // note we do not support multiline comments spanning muliple lines.
                }
                else if (p.Char() == '}')          // end of interpolation.
                {
                    p.Advance();
                    if (!skipOnly)
                        p.EndStringInterpolation();
                    return;
                }
                else
                {
                    p.Advance();
                }
            }

            if(!skipOnly)
            {
                // end of line: note that we do not support interpolations spanning muliple lines.
                p.EndStringInterpolation();
            }
        }

        private void ScanMultiLineComment(LineProgress p, bool skipOnly = false)
        {
            while (!p.EndOfLine)
            {
                if (p.Char() == '*' && p.NextChar() == '/')
                {
                    p.Advance(2);
                    if(!skipOnly)
                        p.State = State.Default;
                    return;
                }
                else
                {
                    p.Advance();
                }
            }
        }
    }
}