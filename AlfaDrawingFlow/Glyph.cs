using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlfaDrawingFlow
{
	using System.Drawing;
	using System.Threading;

	public class GlyphProperties
	{
		public GlyphProperties()
		{
			m_bounds = default(Rectangle);
		}

		public virtual Rectangle Bounds
		{
			get
			{
				return m_bounds;
			}
			set
			{
				m_bounds = value;
			}
		}

		Rectangle m_bounds;
	}
    

    public class Glyph
    {
		public virtual void Draw(Graphics graphics) { }
		public virtual GlyphProperties Properties { get { return default(GlyphProperties); } }
    }
}
