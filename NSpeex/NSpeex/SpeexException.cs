using System;
using System.Runtime.Serialization;

namespace NSpeex
{
    [Serializable]
    public class SpeexException : Exception
    {
      
        public SpeexException()
        {
        }

        public SpeexException(string message) : base(message)
        {
        }

        public SpeexException(string message, Exception inner) : base(message, inner)
        {
        }

        protected SpeexException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}