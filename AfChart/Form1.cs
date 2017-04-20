using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AlfaFlow;
using AlfaDrawingFlow;
using AlfaDrawingFlow.Glyphs;
using AlfaDrawingFlow.Indicators;

namespace AfChart
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

		Pen p = new Pen(Color.White);
		Pen pp = new Pen(Color.Red) { Width = 2 };
		Font f = new Font("Tahoma", 8F, FontStyle.Bold);
        Brush b = new SolidBrush(Color.Green);

		TemporalValue<double>[] values = new[] 
		{
			new TemporalValue<double>(1001L, 201F),
			new TemporalValue<double>(1201L, 221F),
			new TemporalValue<double>(1401L, 241F),
			new TemporalValue<double>(1801L, 201F),
			new TemporalValue<double>(1901L, 291F),
			new TemporalValue<double>(2001L, 201F),
			new TemporalValue<double>(2101L, 401F),
			new TemporalValue<double>(2201L, 601F),
			new TemporalValue<double>(2301L, 701F),
			new TemporalValue<double>(2401L, 501F),
			new TemporalValue<double>(2501L, 661F),
			new TemporalValue<double>(2701L, 421F),
			new TemporalValue<double>(2801L, 331F),
			new TemporalValue<double>(2901L, 441F),
			new TemporalValue<double>(3001L, 5551F),
		};

        private void Form1_Resize(object sender, EventArgs e)
        {
            IndGraphPanel ind = new IndGraphPanel();

            var ps = new[]
            {
                new IndParameter("Rect", (object)drawingPanel1.Bounds),
                new IndParameter("Bands", new [] {1000L, 1450L, 1750L, 3000L}),
                //new IndParameter("xMin", (long)1000),
                //new IndParameter("xMax", (long)3000),
                new IndParameter("yMin", (float)200),
                new IndParameter("yMax", (float)800),
                new IndParameter("Pen", (object)p),
                new IndParameter("Font", (object)f),
                new IndParameter("Brush", (object)b),
                new IndParameter("Fmt", (object)new StringFormat()),
            };

            ind.SetParameters(ps);
            var gs = ind.Run(drawingPanel1.CreateGraphics());
			if (gs.Length > 0)
			{
				var pts = ind.ConvertTemporalValues(values);
				drawingPanel1.Glyphs = gs;
				((IList<Glyph>)drawingPanel1.Glyphs).Add(new PolyLineGlyph(new PolyLineGlyph.PolyLineProperties()
				{
					Pen = pp, Points = pts
				}));
			}
            drawingPanel1.Invalidate();
        }
    }
}
