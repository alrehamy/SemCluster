using System;

namespace OpenNLP.Util
{
    /// <summary>
    /// Class for storing start and end integer offsets.  
    /// </summary>

    public class Span : IComparable
    {
        /// <summary>
        /// Return the start of a span.
        /// </summary>
        /// <returns> 
        /// the start of a span.
        /// </returns>
        public int Start { get; private set; }

        /// <summary>
        /// Return the end of a span.
        /// </summary>
        /// <returns> 
        /// the end of a span.
        /// </returns>
        public int End { get; private set; }


        /// <summary>Constructs a new Span object.
        /// </summary>
        /// <param name="startOfSpan">
        /// start of span.
        /// </param>
        /// <param name="endOfSpan">
        /// end of span.
        /// </param>
        public Span(int startOfSpan, int endOfSpan)
        {
            Start = startOfSpan;
            End = endOfSpan;
        }

        /// <summary>
        /// Computes the length of the span (end - start)
        /// </summary>
        public int Length()
        {
            return (End - Start);
        }

        /// <summary>
        /// Returns true is the specified span is contained by this span.  
        /// Identical spans are considered to contain each other. 
        /// </summary>
        /// <param name="span">
        /// The span to compare with this span.
        /// </param>
        /// <returns>
        /// true if the specified span is contained by this span; false otherwise. 
        /// </returns>
        public bool Contains(Span span)
        {
            return (Start <= span.Start && span.End <= End);
        }

        /// <summary>
        /// Returns true if the specified span is contained stritly by this span,
        /// ie if the current start if strictly less than the input span's start
        /// OR if the current end if strictly greater than the input span's end.
        /// </summary>
        public bool ContainsStrictly(Span span)
        {
            return this.Contains(span)
                   && (Start < span.Start || span.End < End);
        }

        /// <summary>
        /// Returns true if the specified span intersects with this span.
        /// </summary>
        /// <param name="span">
        /// The span to compare with this span. 
        /// </param>
        /// <returns>
        /// true is the spans overlap; false otherwise. 
        /// </returns>
        public bool Intersects(Span span)
        {
            int spanStart = span.Start;
            //either span's start is in this or this's start is in span
            return (this.Contains(span) || span.Contains(this) ||
                    (Start <= spanStart && spanStart < End ||
                     spanStart <= Start && Start < span.End));
        }

        /// <summary>
        /// Returns true if the specified span crosses this span.
        /// </summary>
        /// <param name="span">
        /// The span to compare with this span.
        /// </param>
        /// <returns>
        /// true if the specified span overlaps this span and contains a non-overlapping section; false otherwise.
        /// </returns>
        public bool Crosses(Span span)
        {
            int spanStart = span.Start;
            //either span's Start is in this or this's Start is in span
            return (!this.Contains(span) && !span.Contains(this) &&
                    (Start <= spanStart && spanStart < End ||
                     spanStart <= Start && Start < span.End));
        }

        public virtual int CompareTo(object o)
        {
            var compareSpan = (Span) o;
            if (Start < compareSpan.Start)
            {
                return -1;
            }
            else if (Start == compareSpan.Start)
            {
                if (End > compareSpan.End)
                {
                    return -1;
                }
                else if (End < compareSpan.End)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 1;
            }
        }

        public override int GetHashCode()
        {
            return ((Start << 16) | (0x0000FFFF | this.End));
        }

        public override bool Equals(object o)
        {
            if (!(o is Span))
            {
                return false;
            }
            var currentSpan = (Span) o;
            return (Start == currentSpan.Start && End == currentSpan.End);
        }

        public override string ToString()
        {
            var buffer = new System.Text.StringBuilder(15);
            return (buffer.Append(Start).Append("..").Append(End).ToString());
        }
    }
}