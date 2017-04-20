using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace AlfaDrawingFlow.Glyphs
{
	public class PolyLineGlyph : Glyph
	{
		public class PolyLineProperties : GlyphProperties
		{
			public PolyLineProperties() : base() {	}
			public Point[] Points { get; set; }
            public Pen Pen { get; set; }
		}


		public PolyLineGlyph()
		{
			m_properties = new PolyLineProperties();
		}
		public PolyLineGlyph(PolyLineProperties properties)
		{
			m_properties = properties;
		}

		public override void Draw(Graphics graphics) 
		{
			graphics.DrawLines(m_properties.Pen, m_properties.Points);
		}

		public override GlyphProperties Properties
		{
			get { return m_properties; }
		}

		PolyLineProperties m_properties;
	}
}
