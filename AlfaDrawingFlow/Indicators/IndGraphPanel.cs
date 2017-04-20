using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using AlfaFlow;
using AlfaDrawingFlow;
using AlfaDrawingFlow.Glyphs;

namespace AlfaDrawingFlow.Indicators
{
	public struct TemporalAggregator
	{
		internal struct Band
		{
			public readonly long Start;
			public readonly long End;
            public Band(long end)
            {
                Start = default(long); End = end;
            }
            public Band(long start, long end)
			{
				Start = start; End = end;
			}
			public long Width { get { return End - Start; } }
		}
        internal class BandComparer : IComparer<Band>
        {
            public int Compare(Band x, Band y)
            {
                long xy = x.End - y.End;
                long yx = y.End - x.End;
                return (int)(xy >> 63) | (int)((ulong)yx >> 63);
            }
        }
		public long Start { get { return m_start; } }
		public long End { get { return m_end; } }

        public long this[long value]
        {
            get
            {
                if (m_bands.Count == 1)
                    return (Start >= value) ? Start : value;

                var bands = m_bands.GetViewBetween(new Band(value), new Band(m_end));
                if (bands.Count == 0) return 0;
                long start = bands.Min.Start;
                return (start >= value) ? start : value;
            }
        }

		public long this[float value]
		{
			get 
			{
                if (value > 1 || value < 0) return 0;
				return this[m_start + (long)Math.Floor((value * (float) (m_end - m_start)))];                
			}
		}

		public long[] Bands 
		{ 
			get 
			{
				var result = new long[m_bands.Count * 2];
                int j = 0;
				foreach(var band in m_bands)
				{			
					result[j++] = band.Start;
					result[j++] = band.End;
				}
				return result; 
			}
			set
			{
				if (value == null || value.Length == 0 || value.Length %2 == 1) return;

				Array.Sort(value);
				m_start = value[0]; m_end = value[value.Length - 1];

                m_bands = new SortedSet<Band>(new BandComparer());

				for (int i = 0; i < value.Length; i+= 2)
				{
					m_bands.Add(new Band(value[i], value[i+1]));
				}
			}
		}

		long m_start, m_end;
        SortedSet<Band> m_bands;
	}

    public class IndGraphPanel : IParameterized, IDisposable
    {
		public Point[] ConvertTemporalValues(TemporalValue<double>[] points)
		{
			List<Point> result = new List<Point>();

			float fWidth = x_max - x_min;
			float fHeight = y_max - y_min;

			for (int i = 0; i < points.Length; i++)
			{ 
				TemporalValue<double> v = points[i];
				if (v.TimeStamp >= x_min && x_max >= v.TimeStamp && v.Value >= y_min && y_max >= v.Value)
				{
					int x = m_drect.X + (int)((v.TimeStamp - x_min) * m_drect.Width / fWidth);
					int y = m_drect.Y + (int)((y_max - v.Value) * m_drect.Height / fHeight);
					result.Add(new Point(x, y));
				}
			}
			return result.ToArray();
		}

        public Glyph[] Run(Graphics g)
        {

			var marginSizeX = (new TextGlyph.TextProperties()
			{
				Text = x_max.ToString(),
				Font = m_font,
				Brush = m_brush,
				Location = new Point(1, 1),
				StringFormat = m_format
			}).GetBounds(g).Size;

            var marginSizeY = (new TextGlyph.TextProperties()
                {
                    Text = GetMaxText(y_min.ToString("n4"), y_max.ToString("n4")),
                    Font = m_font,
                    Brush = m_brush,
                    Location = new Point(1, 1),
                    StringFormat = m_format
                }).GetBounds(g).Size;

			m_drect = new Rectangle(m_rect.Location,
					new Size(m_rect.Width - marginSizeY.Width, m_rect.Height - marginSizeY.Height));

			var lines = GetLineProperties(marginSizeY);
			var texts = GetTextProperties(lines, marginSizeX, marginSizeY);

            List<Glyph> gList = new List<Glyph>(lines.Length + texts.Length);            

            gList.AddRange(lines.Select(lp => new LineGlyph(lp)));
			gList.AddRange(texts.Select(tp => new TextGlyph(tp)));

            return gList.ToArray();
        }

		string GetMaxText(string s1, string s2)
		{
			if (s1.Length > s2.Length) return s1;
			return s2;
		}


		internal class LinePropertyComparer : IComparer<LineGlyph.LineProperties>
		{
			public int Compare(LineGlyph.LineProperties x, LineGlyph.LineProperties y)
			{
				if (x.Start.Y == y.Start.Y)	return x.Start.X - y.Start.X;
				return x.Start.Y - y.Start.Y;
			}
		}
		TextGlyph.TextProperties[] GetTextProperties(LineGlyph.LineProperties[] lines, Size marginSizeX, Size marginSizeY)
		{
			if (lines.Length == 0) return new TextGlyph.TextProperties[0];

			float fWidth = x_max - x_min;
			float fHeight = y_max - y_min;

			if (fWidth == 0 || fHeight == 0) return new TextGlyph.TextProperties[0];

			List<TextGlyph.TextProperties> result = new List<TextGlyph.TextProperties>(lines.Length);

			Array.Sort(lines, new LinePropertyComparer());

			int lastTextRightBound = 0, lastTextBottomBound = 0;
			foreach (var lp in lines)
			{
				if (lp.Start.X == lp.End.X) //|-line
				{
					long value = m_ta[(lp.End.X - m_drect.Location.X) / (float)m_drect.Width];
					Point location = new Point(lp.End.X - marginSizeX.Width /2, lp.End.Y + 1);

					if (lastTextRightBound == 0) lastTextRightBound = location.X + marginSizeX.Width;
					else
					{
						if (lastTextRightBound + 5 >= location.X) continue;
						lastTextRightBound = location.X + marginSizeX.Width;
					}

					result.Add(
						new TextGlyph.TextProperties()
						{
							Text = value.ToString(),
							Font = m_font,
							Brush = m_brush,
							Location = location,
							StringFormat = m_format,
							Bounds = new Rectangle(location, marginSizeX)
						});

					continue;
				}

				if (lp.Start.Y == lp.End.Y) //---line
				{
					float value = y_max - fHeight * (lp.End.Y - m_drect.Location.Y) / (float)m_drect.Height;
					Point location = new Point(lp.End.X + 1, lp.End.Y - marginSizeY.Height / 2);

					if (lastTextBottomBound == 0) lastTextBottomBound = location.Y + marginSizeY.Height;
					else
					{
						if (lastTextBottomBound + 5 >= location.Y) continue;
						lastTextBottomBound = location.Y + marginSizeY.Height;
					}

					result.Add(
						new TextGlyph.TextProperties()
						{
							Text = value.ToString("n4"),
							Font = m_font,
							Brush = m_brush,
							Location = location,
							StringFormat = m_format,
							Bounds = new Rectangle(location, marginSizeY)
						});
				}
			}
			return result.ToArray();
		}

		LineGlyph.LineProperties[] GetLineProperties(Size marginSize)
		{
			int hCount = (int)Math.Floor(Math.Log(m_drect.Height, 2));
			int wCount = (int)Math.Floor(Math.Log(m_drect.Width, 2));

			if (hCount == 0 && wCount == 0)
				return new LineGlyph.LineProperties[0];

			List<LineGlyph.LineProperties> result = new List<LineGlyph.LineProperties>();
			int index = 0;
			if (hCount > 0)
			{
				int hStep = Math.Max(20, m_drect.Height / hCount);
				for (int y = m_drect.Height / 2; y < m_drect.Height - 10; y += hStep, index++)
				{
					result.Add(
						new LineGlyph.LineProperties()
						{
							Pen = m_pen,
							Start = new Point(m_drect.Location.X, y),
							End = new Point(m_drect.Location.X + m_drect.Size.Width, y)
						});
				}

				for (int y = m_drect.Height / 2 - hStep; y > 10; y -= hStep, index++)
				{
					result.Add(
						new LineGlyph.LineProperties()
						{
							Pen = m_pen,
							Start = new Point(m_drect.Location.X, y),
							End = new Point(m_drect.Location.X + m_drect.Size.Width, y)
						});
				}
			}

			if (wCount > 0)
			{
				int wStep = Math.Max(20, m_drect.Width / wCount);

				for (int x = m_drect.Width / 2; x < m_drect.Width - 10; x += wStep, index++)
				{
					result.Add(
						new LineGlyph.LineProperties()
						{
							Pen = m_pen,
							Start = new Point(x, m_drect.Location.Y),
							End = new Point(x, m_drect.Location.Y + m_drect.Size.Height)
						});

				}

				for (int x = m_drect.Width / 2 - wStep; x > 10; x -= wStep, index++)
				{
					result.Add(
						new LineGlyph.LineProperties()
						{
							Pen = m_pen,
							Start = new Point(x, m_drect.Location.Y),
							End = new Point(x, m_drect.Location.Y + m_drect.Size.Height)
						});
				}
			}

			return result.ToArray();
		}

        #region IParametrized
        public IndParameter[] GetParameters()
        {
            return new[]
            {
                new IndParameter("Rect", (object)m_rect),
                new IndParameter("xMin", (object)x_min),
                new IndParameter("xMax", (object)x_max),
                new IndParameter("Bands", m_ta.Bands),
                new IndParameter("yMin", (object)y_min),
                new IndParameter("yMax", (object)y_max),
                new IndParameter("Pen", (object)m_pen),
                new IndParameter("Font", (object)m_font),
                new IndParameter("Brush", (object)m_brush),
                new IndParameter("Fmt", (object)m_format),
            };
        }

        public void SetParameters(params IndParameter[] parameters)
        {
            foreach (var parameter in parameters)
            {
                switch((string)parameter)
                {
                    case "Rect":
                        m_rect = (Rectangle)parameter.Value;
						m_drect = m_rect;
                        break;
                    case "Bands":
                        m_ta = new TemporalAggregator() { Bands = (long[])parameter.Value };
                        x_min = m_ta.Start; 
                        x_max = m_ta.End;
                        break;
                    case "xMin":
                        x_min = parameter;
                        if (x_max > x_min)
                            m_ta = new TemporalAggregator() { Bands = new [] {x_min, x_max} };
                        break;
                    case "xMax":
                        x_max = parameter;
                        if (x_max > x_min)
                            m_ta = new TemporalAggregator() { Bands = new[] { x_min, x_max } };
                        break;
                    case "yMin":
                        y_min = parameter;
                        break;
                    case "yMax":
                        y_max = parameter;
                        break;
                    case "Pen":
                        m_pen = (Pen)parameter.Value;
                        break;
                    case "Font":
                        m_font = (Font)parameter.Value;
                        break;
                    case "Brush":
                        m_brush = (Brush)parameter.Value;
                        break;
                    case "Fmt":
                        m_format = (StringFormat)parameter.Value;
                        break;
                }
            }
        }

        Pen m_pen;
        Font m_font;
        Brush m_brush;
        StringFormat m_format;
        Rectangle m_rect;
		Rectangle m_drect;
        long x_min, x_max;
        float y_min, y_max;
        TemporalAggregator m_ta;
        #endregion

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
