using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using AlfaDrawingFlow.Glyphs;

namespace AlfaDrawingFlow
{
	public partial class DrawingPanel : UserControl
	{
		public DrawingPanel()
		{
			InitializeComponent();
			m_glyphs = new List<Glyph>();
		}

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IEnumerable<Glyph> Glyphs
		{
			get 
			{
				return m_glyphs;
			}

			set 
			{
                m_glyphs = new List<Glyph>(value?? new List<Glyph>());
            }
		}

        Pen p = new Pen(Color.White);

        protected override void OnPaint(PaintEventArgs e)
        {
            if (InvokeRequired) return;
            if (DesignMode)
            {
                base.OnPaintBackground(e);
                return;
            }

            e.Graphics.Clear(Color.Black);

            if (m_glyphs != null)
            {
                foreach (var glyph in m_glyphs)
                {
                    try
                    {
                        glyph.Draw(e.Graphics);
                    }
                    catch(Exception)
                    {
                    }
                }
            }
        }

		List<Glyph> m_glyphs;

		public static LineGlyph.LineProperties[] GetLineGrid(Rectangle rect, Pen pen = null)
		{
			int hCount = (int)Math.Floor(Math.Log(rect.Height, 2));
			int wCount = (int)Math.Floor(Math.Log(rect.Width, 2));

			if (hCount == 0 && wCount == 0) return new LineGlyph.LineProperties[0];

			List<LineGlyph.LineProperties> result = new List<LineGlyph.LineProperties>();

			int index = 0;
			if (hCount > 0)
			{
				int hStep = Math.Max(20, rect.Height / hCount);

				for (int y = rect.Height /2; y < rect.Height - 10; y += hStep, index++)
				{
					result.Add(
                        new LineGlyph.LineProperties()
					    {
                            Pen = pen,
						    Start = new Point(rect.Location.X, y),
						    End = new Point(rect.Location.X + rect.Size.Width, y)
					    });
				}

				for (int y = rect.Height /2 - hStep; y > 10; y -= hStep, index++)
				{
					result.Add(
                        new LineGlyph.LineProperties()
					    {
                            Pen = pen,
                            Start = new Point(rect.Location.X, y),
						    End = new Point(rect.Location.X + rect.Size.Width, y)
					    });
				}
			}

			if (wCount > 0)
			{
				int wStep = Math.Max(20, rect.Width / wCount);

				for (int x = rect.Width / 2; x < rect.Width - 10; x += wStep, index++)
				{
					result.Add(
                        new LineGlyph.LineProperties()
					    {
                            Pen = pen,
						    Start = new Point(x, rect.Location.Y),
						    End = new Point(x, rect.Location.Y + rect.Size.Height)
					    });
				}

				for (int x = rect.Width / 2 - wStep; x > 10; x -= wStep, index++)
				{
					result.Add( 
                        new LineGlyph.LineProperties()
					    {
                            Pen = pen,
						    Start = new Point(x, rect.Location.Y),
						    End = new Point(x, rect.Location.Y + rect.Size.Height)
					    });
				}
			}

			return result.ToArray();
		}

	}
}
