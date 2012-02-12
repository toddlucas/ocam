using System;

namespace Ocam
{
    public class PageProcessingException : Exception
    {
        string _filename;
        string _text;
        public string FileName { get { return _filename; } }
        public string Text { get { return _text; } }

        public PageProcessingException(string message, string filename, string text, Exception innerException) : 
            base(message, innerException) 
        {
            _filename = filename;
            _text = text;
        }
    }
}
