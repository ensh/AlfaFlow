using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace AlfaDrawingFlow.Glyphs
{
    public class TextGlyph : Glyph
    {
        public class TextProperties : GlyphProperties
        {
            public TextProperties() : base() 
            {
                StringFormat = new StringFormat();
            }

            public Rectangle GetBounds(Graphics g)
            {
                var sizef = g.MeasureString(Text, Font);
                return new Rectangle(Location, new Size((int)sizef.Width, (int)sizef.Height));
            }

            public string Text { get; set; }
            public Point Location { get; set; }
            public Brush Brush { get; set; }
            public Font Font { get; set; }
            public StringFormat StringFormat { get; set; }
        }


        public TextGlyph()
		{
			m_properties = new TextProperties();
		}

		public TextGlyph(TextProperties properties)
		{
			m_properties = properties;
		}

		public override void Draw(Graphics graphics) 
		{
			graphics.DrawString(m_properties.Text, m_properties.Font, m_properties.Brush, 
                m_properties.Location.X, m_properties.Location.Y, m_properties.StringFormat);
		}

		public override GlyphProperties Properties
		{
			get { return m_properties; }
		}

		TextProperties m_properties;
    }
}
