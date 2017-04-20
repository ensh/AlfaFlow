using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace AlfaDrawingFlow.Glyphs
{
	using AlfaFlow;

	public class LineGlyph : Glyph
	{
		public class LineProperties : GlyphProperties
		{
			public LineProperties() : base() {	}
			public override Rectangle Bounds
			{
				get 
				{
					return new Rectangle(Start, new Size(End.X - Start.X, End.Y - Start.Y));
				}
				set 
				{
					if (Bounds != value)
					{
						Start = value.Location;
						End = new Point(value.Location.X + value.Size.Width, value.Location.Y + value.Size.Height);
					}
				}
			}
			public Point Start { get; set; }
			public Point End { get; set; }
            public Pen Pen { get; set; }
		}


		public LineGlyph()
		{
			m_properties = new LineProperties();
		}
		public LineGlyph(LineProperties properties)
		{
			m_properties = properties;
		}

		public override void Draw(Graphics graphics) 
		{
			graphics.DrawLine(m_properties.Pen, m_properties.Start, m_properties.End);
		}

		public override GlyphProperties Properties
		{
			get { return m_properties; }
		}

		LineProperties m_properties;
	}
}
