﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CuckooSearch;

namespace CuckooFlights
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		#region DI

		public MainWindow()
		{
			InitializeComponent();
		}

		#endregion

		#region Radio Buttons

		private void SphereRb_Checked(object sender, RoutedEventArgs e)
		{
			DrawFunction(new Function
			{
				Expression = x => { return x.Sum(t => t * t); },
				BoundLower = -100,
				BoundUpper = 100,
				Dimensions = 2,
				LambdaMax = 3,
				LambdaMin = 0.1,
			});
		}

		private void AckleyRb_Checked(object sender, RoutedEventArgs e)
		{
			DrawFunction(new Function
			{
				Expression = x =>
				{
					double _1NaD = 1.0 / x.Count;
					return -20 * Math.Exp(-0.2 * Math.Sqrt(_1NaD * x.Sum(t => t * t))) -
						Math.Exp(_1NaD * x.Sum(t => Math.Cos(2 * Math.PI * t))) + 20 + Math.E;
				},
				BoundLower = -32.768,
				BoundUpper = 32.768,
				Dimensions = 20
			});
		}

		private void GriewankRb_Checked(object sender, RoutedEventArgs e)
		{
			DrawFunction(new Function
			{
				Expression = x =>
				{
					double mul = 1.0;
					for (int i = 0; i < x.Count; i++)
						mul *= Math.Cos(x[i] / Math.Sqrt(i + 1));
					return x.Sum(t => t * t / 4000) - mul + 1;
				},
				BoundLower = -5,
				BoundUpper = 5,
				Dimensions = 50
			});
		}

		private void RastriginRb_Checked(object sender, RoutedEventArgs e)
		{
			DrawFunction(new Function
			{
				Expression = x => { return x.Sum(t => t * t - 10 * Math.Cos(2 * Math.PI * t) + 10); },
				BoundLower = -5.12,
				BoundUpper = 5.12,
				Dimensions = 30,
				IterationsNumber = 150000
			});
		}

		private void RosenbrockRb_Checked(object sender, RoutedEventArgs e)
		{
			DrawFunction(new Function
			{
				Expression = x =>
				{
					double res = .0;
					for (int i = 0; i < x.Count - 1; i++)
						res += 100 * Math.Pow(x[i + 1] - x[i] * x[i], 2) + (x[i] - 1) * (x[i] - 1);
					return res;
				},
				BoundLower = -5,
				BoundUpper = 10,
				Dimensions = 30
			});
		}

		#endregion

		private void DrawFunction(Function function)
		{
			int width = Convert.ToInt32(FunctionImage.Height);
			int height = Convert.ToInt32(FunctionImage.Width);
			(double min, double max) = GetFunctionMinMax(function, width, height);
			
			double stepWidth = Math.Abs(function.BoundUpper - function.BoundLower) / width;
			double stepHeight = Math.Abs(function.BoundUpper - function.BoundLower) / height;
			// var pixels = new byte[height, width, 4];
			var pixels1d = new byte[height * width * 4];
			int pixelsIndex = 0;
			for (double x = function.BoundLower; x < function.BoundUpper; x += stepWidth)
			{
				for (double y = function.BoundLower; y < function.BoundUpper; y += stepHeight)
				{
					Color color = GetColor(function, max, min, x, y);
					pixels1d[pixelsIndex++] = color.R;
					pixels1d[pixelsIndex++] = color.G;
					pixels1d[pixelsIndex++] = color.B;
					pixels1d[pixelsIndex++] = color.A;
				}
			}
			
			var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
			var rect = new Int32Rect(0, 0, width, height);
			int stride = 4 * width;
			bitmap.WritePixels(rect, pixels1d, stride, 0);
			FunctionImage.Source = bitmap;
		}

		private static Color GetColor(Function function, double max, double min, double x, double y)
		{
			double value = function.Expression(new List<double> {x, y});

			double dist = Math.Abs(max - min);
			double colorDist = dist / 4;
			double r = max;
			double gb = min + colorDist;
			double g = min + 2 * colorDist;
			double rg = min + 3 * colorDist;
			double b = min;

			// G: 0 to 255
			if (b <= value && value < gb)
			{
				double up = value - b;
				double scale = up / colorDist;
				return new Color {A = byte.MaxValue, R = 0, G = Convert.ToByte(scale * 255), B = 255};
			}

			// B: 255 to 0
			if (gb <= value && value < g)
			{
				double up = value - gb;
				double scale = up / colorDist;
				return new Color {A = byte.MaxValue, R = 0, G = 255, B = Convert.ToByte((1 - scale) * 255)};
			}

			// R: 0 to 255
			if (g <= value && value < rg)
			{
				double up = value - g;
				double scale = up / colorDist;
				return new Color {A = byte.MaxValue, R = Convert.ToByte(scale * 255), G = 0, B = 0};
			}

			// G: 255 to 0
			if (rg <= value && value <= r)
			{
				double up = value - rg;
				double scale = up / colorDist;
				return new Color {A = byte.MaxValue, R = 255, G = Convert.ToByte((1 - scale) * 255), B = 0};
			}

			return Colors.White;
		}

		private static Tuple<double, double> GetFunctionMinMax(Function function, int width, int height)
		{
			double min = function.Expression(new List<double> {function.BoundLower, function.BoundLower});
			double max = min;

			double stepWidth = Math.Abs(function.BoundUpper - function.BoundLower) / width;
			double stepHeight = Math.Abs(function.BoundUpper - function.BoundLower) / height;
			for (double x = function.BoundLower; x <= function.BoundUpper; x += stepWidth)
			{
				for (double y = function.BoundLower; y <= function.BoundUpper; y += stepHeight)
				{
					double value = function.Expression(new List<double> {x, y});
					if (value < min)
					{
						min = value;
					}

					if (max < value)
					{
						max = value;
					}
				}
			}
			
			return new Tuple<double, double>(min, max);
		}
	}
}