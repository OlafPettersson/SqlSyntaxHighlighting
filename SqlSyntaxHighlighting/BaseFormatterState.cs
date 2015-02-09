using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    internal class BaseFormatterState
    {
        protected IList<KeyValuePair<string, string>> _output = new List<KeyValuePair<string, string>>();

        public virtual void AddOutputContent(string content)
        {
            AddOutputContent(content, null);
        }

        public virtual void AddOutputContent(string content, string htmlClassName)
        {
            _output.Add(new KeyValuePair<string, string>(htmlClassName, content));
        }

        public virtual void OpenClass(string htmlClassName)
        {
            //if (htmlClassName == null)
            //    throw new ArgumentNullException("htmlClassName");

            //if (HtmlOutput)
            //    _outBuilder.Append(@"<span class=""" + htmlClassName + @""">");
        }

        public virtual void CloseClass()
        {
            //if (HtmlOutput)
            //    _outBuilder.Append(@"</span>");
        }

        public virtual void AddOutputContentRaw(string content)
        {
            //_outBuilder.Append(content);
        }

        public virtual void AddOutputLineBreak()
        {
            //_outBuilder.Append(Environment.NewLine);
        }

        public IEnumerable<KeyValuePair<string, string>> DumpOutput()
        {
            return _output;
        }

    }

}
