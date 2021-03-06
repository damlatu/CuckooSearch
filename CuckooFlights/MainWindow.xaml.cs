﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CuckooSearch;

namespace CuckooFlights
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		#region DI

		private const int NestsNumber = 15;
		private readonly List<Ellipse> _nests = new List<Ellipse>();

		private Function _function;
		private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

		private readonly SolidColorBrush _brushBlack = Brushes.Black;
		private readonly SolidColorBrush _brushWhite = Brushes.White;
		private const int LabelWidth = 50;

		public MainWindow()
		{
			InitializeComponent();
		}

		#endregion

		#region Controls

		#region Radio Buttons

		private void SphereRb_Checked(object sender, RoutedEventArgs e)
		{
			_function = new Function
			{
				Expression = x => { return x.Sum(t => t * t); },
				BoundLower = -100,
				BoundUpper = 100,
				Dimensions = 2
			};
			DrawFunction();
		}

		private void AckleyRb_Checked(object sender, RoutedEventArgs e)
		{
			_function = new Function
			{
				Expression = x =>
				{
					double _1NaD = 1.0 / x.Count;
					return -20 * Math.Exp(-0.2 * Math.Sqrt(_1NaD * x.Sum(t => t * t))) -
						Math.Exp(_1NaD * x.Sum(t => Math.Cos(2 * Math.PI * t))) + 20 + Math.E;
				},
				BoundLower = -32.768,
				BoundUpper = 32.768,
				Dimensions = 2
			};
			DrawFunction();
		}

		private void GriewankRb_Checked(object sender, RoutedEventArgs e)
		{
			_function = new Function
			{
				Expression = x =>
				{
					double mul = 1.0;
					for (int i = 0; i < x.Count; i++)
						mul *= Math.Cos(x[i] / Math.Sqrt(i + 1));
					return x.Sum(t => t * t / 4000) - mul + 1;
				},
				BoundLower = -100,
				BoundUpper = 100,
				Dimensions = 2
			};
			DrawFunction();
		}

		private void RastriginRb_Checked(object sender, RoutedEventArgs e)
		{
			_function = new Function
			{
				Expression = x => { return 10 * x.Count + x.Sum(t => t * t - 10 * Math.Cos(2 * Math.PI * t)); },
				BoundLower = -5,
				BoundUpper = 5,
				Dimensions = 2,
				IterationsNumber = 150000
			};
			DrawFunction();
		}

		private void RosenbrockRb_Checked(object sender, RoutedEventArgs e)
		{
			_function = new Function
			{
				Expression = x =>
				{
					double res = .0;
					for (int i = 0; i < x.Count - 1; i++)
					{
						res += 100 * Math.Pow(x[i + 1] - x[i] * x[i], 2) + (x[i] - 1) * (x[i] - 1);
					}

					return res;
				},
				BoundLower = -2.048,
				BoundUpper = 2.048,
				Dimensions = 2
			};
			DrawFunction();
		}

		private void ManualRb_Checked(object sender, RoutedEventArgs e)
		{
			AlphaLabel.IsEnabled = true;
			Alpha.IsEnabled = true;
			LambdaLabel.IsEnabled = true;
			Lambda.IsEnabled = true;
		}

		private void AutomaticRb_Checked(object sender, RoutedEventArgs e)
		{
			AlphaLabel.IsEnabled = false;
			Alpha.IsEnabled = false;
			LambdaLabel.IsEnabled = false;
			Lambda.IsEnabled = false;
		}

		#endregion

		#region Buttons

		private async void RunButton_Click(object sender, RoutedEventArgs e)
		{
			Reset();

			ResetButton.IsEnabled = true;
			RunButton.IsEnabled = false;
			await RunAlgorithm(_cancellationTokenSource.Token);
			RunButton.IsEnabled = true;
			ResetButton.IsEnabled = false;
		}

		private void ResetButton_Click(object sender, RoutedEventArgs e)
		{
			Reset();
			RunButton.IsEnabled = true;
		}

		#endregion

		#region Other

		private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			Reset();
			if (_function != null)
			{
				DrawFunction();
			}
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			Keyboard.ClearFocus();
		}

		private void Lambda_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			var approvedDecimalPoint = false;

			if (e.Text == "." || e.Text == ",")
			{
				if (!((TextBox)sender).Text.Contains(".") && !((TextBox)sender).Text.Contains(","))
				{
					approvedDecimalPoint = true;
				}
			}

			if (!(char.IsDigit(e.Text, e.Text.Length - 1) || approvedDecimalPoint))
			{
				e.Handled = true;
			}
		}

		#endregion

		#endregion

		#region Graphics

		private void DrawFunction()
		{
			Reset();

			int canvasHeight = Convert.ToInt32(Canvas.ActualHeight);
			int canvasWidth = Convert.ToInt32(Canvas.ActualWidth);
			int height = Math.Min(canvasHeight, canvasWidth);
			if (height % 2 == 0)
			{
				height--;
			}

			int width = height;
			(double min, double max) = GetFunctionMinMax(_function, width, height);

			var pixels1d = new byte[height * width * 4];
			int pixelsIndex = 0;
			for (int h = 0; h < height; h++)
			{
				double y = TransformPixelToY(h, height);
				for (int w = 0; w < width; w++)
				{
					double x = TransformPixelToX(w, width);

					Color color = GetColor(_function, max, min, x, y);
					pixels1d[pixelsIndex++] = color.B;
					pixels1d[pixelsIndex++] = color.G;
					pixels1d[pixelsIndex++] = color.R;
					pixels1d[pixelsIndex++] = GetOpacity(_function, x, y);
				}
			}

			FunctionImage.Height = height;
			FunctionImage.Width = width;
			var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
			var rect = new Int32Rect(0, 0, width, height);
			int stride = 4 * width;
			bitmap.WritePixels(rect, pixels1d, stride, 0);
			FunctionImage.Source = bitmap;

			FunctionImage.Margin = new Thickness((Window.ActualWidth - Canvas.Margin.Left - width) * .5,
				(canvasHeight - height) * .5,
				FunctionImage.Margin.Right,
				FunctionImage.Margin.Bottom);
			Panel.SetZIndex(FunctionImage, 1);

			LabelUpperY.Content = _function.BoundUpper;
			LabelLowerY.Content = _function.BoundLower;
			double left = FunctionImage.Margin.Left + Canvas.Margin.Left - LabelWidth;
			LabelUpperY.Margin = new Thickness { Left = left };
			LabelLowerY.Margin = new Thickness { Left = left, Bottom = LabelLowerY.Margin.Bottom };

			LabelUpperX.Content = _function.BoundUpper;
			LabelLowerX.Content = _function.BoundLower;
			LabelLowerX.Margin = new Thickness { Left = left + LabelWidth, Bottom = LabelLowerX.Margin.Bottom };
			LabelUpperX.Margin = new Thickness { Left = left + width, Bottom = LabelUpperX.Margin.Bottom };

			CalculateLambda();
			CalculateAlpha();
			RunButton.IsEnabled = true;
			ParamsGrid.IsEnabled = true;
		}

		private static Color GetColor(Function function, double max, double min, double x, double y)
		{
			double value = function.Expression(new List<double> { x, y });

			double dist = Math.Abs(max - min);
			double colorDist = dist / 4;
			double r = max;
			double gb = min + colorDist;
			double g = min + 2 * colorDist;
			double rg = min + 3 * colorDist;
			double b = min;

			// G: 255 to 0
			if (rg <= value && value <= r)
			{
				double up = value - rg;
				double scale = up / colorDist;
				return new Color { A = byte.MaxValue, R = 255, G = Convert.ToByte((1 - scale) * 255), B = 0 };
			}

			// R: 0 to 255
			if (g <= value && value < rg)
			{
				double up = value - g;
				double scale = up / colorDist;
				return new Color { A = byte.MaxValue, R = Convert.ToByte(scale * 255), G = 255, B = 0 };
			}

			// B: 255 to 0
			if (gb <= value && value < g)
			{
				double up = value - gb;
				double scale = up / colorDist;
				return new Color { A = byte.MaxValue, R = 0, G = 255, B = Convert.ToByte((1 - scale) * 255) };
			}

			// G: 0 to 255
			if ( /*b <= value && */value < gb)
			{
				double up = value - b;
				double scale = up / colorDist;
				return new Color { A = byte.MaxValue, R = 0, G = Convert.ToByte(scale * 255), B = 255 };
			}

			return Colors.White;
		}

		private static byte GetOpacity(Function function, double x, double y)
		{
			// return byte.MaxValue;

			double opacityMargin = Math.Abs(function.BoundUpper - function.BoundLower) * .02;
			double left = function.BoundLower + opacityMargin;
			double right = function.BoundUpper - opacityMargin;
			double top = left;
			double bottom = right;

			byte xOpacity = byte.MaxValue;
			byte yOpacity = byte.MaxValue;

			// L
			if (x <= left)
			{
				double dist = x - function.BoundLower;
				double scale = dist / opacityMargin;
				xOpacity = Convert.ToByte(scale * 255);
			}

			// R
			if (right <= x)
			{
				double dist = function.BoundUpper - x;
				double scale = dist / opacityMargin;
				xOpacity = Convert.ToByte(scale * 255);
			}

			// L
			if (y <= top)
			{
				double dist = y - function.BoundLower;
				double scale = dist / opacityMargin;
				yOpacity = Convert.ToByte(scale * 255);
			}

			// R
			if (bottom <= y)
			{
				double dist = function.BoundUpper - y;
				double scale = dist / opacityMargin;
				yOpacity = Convert.ToByte(scale * 255);
			}

			return Math.Min(xOpacity, yOpacity);
		}

		#endregion

		#region Mathematics

		private double TransformPixelToX(int w, int imageSize)
		{
			double dist = Math.Abs(_function.BoundUpper - _function.BoundLower);

			return _function.BoundLower + w * dist / imageSize;
		}

		private double TransformPixelToY(int h, int imageSize)
		{
			double dist = Math.Abs(_function.BoundUpper - _function.BoundLower);

			return _function.BoundLower + (imageSize - h) * dist / imageSize;
		}

		private double TransformX(Function function, double x)
		{
			double marginLeft = x - function.BoundLower;
			double scale = marginLeft / Math.Abs(function.BoundUpper - function.BoundLower);
			return FunctionImage.Margin.Left + scale * FunctionImage.ActualWidth;
		}

		private double TransformY(Function function, double y)
		{
			double marginTop = y - function.BoundLower;
			double scale = marginTop / Math.Abs(function.BoundUpper - function.BoundLower);
			return FunctionImage.Margin.Top + scale * FunctionImage.ActualWidth;
		}

		private static Tuple<double, double> GetFunctionMinMax(Function function, int width, int height)
		{
			double min = function.Expression(new List<double> { function.BoundLower, function.BoundLower });
			double max = min;

			double stepWidth = Math.Abs(function.BoundUpper - function.BoundLower) / (width - 1);
			double stepHeight = Math.Abs(function.BoundUpper - function.BoundLower) / (height - 1);
			for (double x = function.BoundLower; x <= function.BoundUpper; x += stepWidth)
			{
				for (double y = function.BoundLower; y <= function.BoundUpper; y += stepHeight)
				{
					double value = function.Expression(new List<double> { x, y });
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

		private double CalculateAlpha()
		{
			if (ManualRb.IsChecked == true)
			//if (_lambdaChangedManually)
			{
				return double.Parse(Alpha.Text.Replace('.', ','));
			}

			// dist = 100+ will be 1.5
			// dist = 0 will be 3
			double lambda = Math.Min(.1 + .9 * (Math.Abs(_function.BoundUpper - _function.BoundLower) / 100), 1);
			Alpha.Text = lambda.ToString("0.00");
			return lambda;
		}

		private double CalculateLambda()
		{
			if (ManualRb.IsChecked == true)
			//if (_lambdaChangedManually)
			{
				return double.Parse(Lambda.Text.Replace('.', ','));
			}

			// dist = 100+ will be 1
			// dist = 0 will be 0.1
			double lambda = Math.Max(3 - 1.5 * (Math.Abs(_function.BoundUpper - _function.BoundLower) / 100), 1.5);
			Lambda.Text = lambda.ToString("0.00");
			return lambda;
		}

		#endregion

		#region Actions

		private async Task RunAlgorithm(CancellationToken cancellationToken)
		{
			var algorithm = new Algorithm();
			algorithm.Initialize(NestsNumber, 1, _function, CalculateAlpha(), CalculateLambda());

			for (int i = 0; i < NestsNumber; i++)
			{
				Bird host = algorithm.Population.Hosts[i];
				var nest = new Ellipse
				{
					Height = 5,
					Width = 5,
					StrokeThickness = 1,
					Stroke = _brushWhite
				};
				_nests.Add(nest);
				nest.SetValue(Canvas.LeftProperty, TransformX(_function, host.X[0]) - 2);
				nest.SetValue(Canvas.TopProperty, TransformY(_function, host.X[1]) - 2);
				Canvas.Children.Add(nest);
				Panel.SetZIndex(Canvas.Children[^1], 3);
			}

			Bird cuckoo = algorithm.Population.Cuckoos.First();
			double prevX = cuckoo.X[0];
			double prevY = cuckoo.X[1];
			for (int iter = 1; iter <= _function.IterationsNumber; iter++)
			{
				if (cancellationToken.IsCancellationRequested)
				{
					return;
				}

				algorithm.Iteration(_function);

				double x = cuckoo.X[0];
				double y = cuckoo.X[1];
				Canvas.Children.Add(new Line
				{
					X1 = TransformX(_function, prevX),
					X2 = TransformX(_function, x),
					Y1 = TransformY(_function, prevY),
					Y2 = TransformY(_function, y),
					StrokeThickness = 1,
					Stroke = _brushBlack
				});
				Panel.SetZIndex(Canvas.Children[^1], 2);

				for (int i = 0; i < NestsNumber; i++)
				{
					Bird host = algorithm.Population.Hosts[i];
					Ellipse nest = _nests[i];
					nest.SetValue(Canvas.LeftProperty, TransformX(_function, host.X[0]) - 2);
					nest.SetValue(Canvas.TopProperty, TransformY(_function, host.X[1]) - 2);
				}

				prevX = x;
				prevY = y;
				await Task.Delay(10);
			}
		}

		private void Reset()
		{
			_cancellationTokenSource.Cancel();
			_nests.Clear();
			Canvas.Children.RemoveRange(1, Canvas.Children.Count - 1);
			_cancellationTokenSource = new CancellationTokenSource();
			//_lambdaChangedManually = false;
		}

		#endregion
	}
}