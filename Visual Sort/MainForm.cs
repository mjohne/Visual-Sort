﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

using Visual_Sort.Properties;

namespace Visual_Sort
{
	public partial class MainForm : Form
	{
		private long
			comparisons = 0,
			swaps = 0;

		private readonly byte[] array = new byte[byte.MaxValue];

		private readonly Stopwatch watch = new Stopwatch();

		private Graphics graphics;

		private readonly Random rand = new Random();

		private readonly Pen
			penDraw = new Pen(SystemColors.ControlText, 1),
			penMarker = new Pen(Color.OrangeRed, 1),
			penFinal = new Pen(Color.LimeGreen, 1),
			penControl = new Pen(Color.White, 1);

		private readonly SolidBrush
			brushDraw = new SolidBrush(SystemColors.ControlText),
			brushMarker = new SolidBrush(Color.OrangeRed),
			brushFinal = new SolidBrush(Color.LimeGreen),
			brushControl = new SolidBrush(Color.White);

		private bool isShuffled = false;

		private Thread thread;

		private readonly Bitmap bmpSave;

		private readonly Dictionary<string, string> dicLogging = new Dictionary<string, string>();

		#region Assemblyattributaccessoren

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public string GetAssemblyTitle()
		{
			object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(attributeType: typeof(AssemblyTitleAttribute), inherit: false);
			if (attributes.Length > 0)
			{
				AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
				if (titleAttribute.Title != "")
				{
					return titleAttribute.Title;
				}
			}
			return System.IO.Path.GetFileNameWithoutExtension(path: Assembly.GetExecutingAssembly().CodeBase);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public string GetAssemblyVersion() => Assembly.GetExecutingAssembly().GetName().Version.ToString();

		/// <summary>
		/// 
		/// </summary>
		public string GetAssemblyDescription()
		{
			object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(attributeType: typeof(AssemblyDescriptionAttribute), inherit: false);
			if (attributes.Length == 0)
			{
				return "";
			}
			return ((AssemblyDescriptionAttribute)attributes[0]).Description;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public string GetAssemblyProduct()
		{
			object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(attributeType: typeof(AssemblyProductAttribute), inherit: false);
			if (attributes.Length == 0)
			{
				return "";
			}
			return ((AssemblyProductAttribute)attributes[0]).Product;
		}

		/// <summary>
		/// 
		/// </summary>
		public string GetAssemblyCopyright()
		{
			object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(attributeType: typeof(AssemblyCopyrightAttribute), inherit: false);
			if (attributes.Length == 0)
			{
				return "";
			}
			return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public string GetAssemblyCompany()
		{
			object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(attributeType: typeof(AssemblyCompanyAttribute), inherit: false);
			if (attributes.Length == 0)
			{
				return "";
			}
			return ((AssemblyCompanyAttribute)attributes[0]).Company;
		}
		#endregion

		public MainForm() => InitializeComponent();

		private void InitArray()
		{
			for (byte i = 0; i < array.Length; i++)
			{
				array[i] = i;
			}
		}

		private void InitArrayReverse()
		{
			for (byte i = 0; i < array.Length; i++)
			{
				array[array.Length - i] = i;
			}
		}

		private void MeasureTime() => labelRuntimeValue.Text = watch.Elapsed.ToString();

		private void ShowProcessingInformation()
		{
			if (checkBoxDataProcessingSpeed.Checked)
			{
				labelComparisonValue.Text = comparisons.ToString() + (comparisons / watch.Elapsed.TotalSeconds).ToString("' ('0.00 'per sec)'");
				labelSwapValue.Text = swaps.ToString() + (swaps / watch.Elapsed.TotalSeconds).ToString("' ('0.00 'per sec)'");
			}
			else
			{
				labelComparisonValue.Text = comparisons.ToString();
				labelSwapValue.Text = swaps.ToString();
			}
			labelCsRelationValue.Text = ((swaps * 1.0) / comparisons).ToString("0.0000");
		}

		private void DoLogging()
		{
			if (checkBoxEnableLogging.Checked)
			{
				dicLogging.Add(watch.Elapsed.ToString(), string.Join(",", array.Select(p => p.ToString()).ToArray()));
			}
		}

		private void ApplyFinalEvent()
		{
			for (byte i = 0; i < array.Length; i++)
			{
				switch (comboBoxVisualizationScheme.SelectedIndex)
				{
					case 0: //lines
						{
							graphics.DrawLine(penFinal, i + 1, panelDraw.Height - array[i], i + 1, panelDraw.Height);
							//graphics.DrawLine(penControl, i + 1, 0, i + 1, panelDraw.Height - array[i]);
							break;
						}
					case 1: //dotes
						{
							graphics.FillRectangle(brushFinal, i + 1, panelDraw.Height - array[i], 1, 1);
							break;
						}
				}
			}
		}

		private delegate void SetControlValueCallback(Control pnlSort);

		private void RefreshPanel(Control pnlSort)
		{
			if (pnlSort.InvokeRequired)
			{
				SetControlValueCallback d = new SetControlValueCallback(RefreshPanel);
				pnlSort.Invoke(d, new object[] { pnlSort });
			}
			else
			{
				pnlSort.Refresh();
			}
		}

		private void DrawArray()
		{
			/*bmpSave = new Bitmap(panelDraw.Width, panelDraw.Height);
			graphics = Graphics.FromImage(bmpSave);
			panelDraw.Image = bmpSave;*/
			if (comboBoxVisualizationScheme.SelectedItem.ToString() != Resources.strLines)
			{
				graphics.Clear(color: panelDraw.BackColor);
			}

			for (byte i = 0; i < array.Length; i++)
			{
				switch (comboBoxVisualizationScheme.SelectedIndex)
				{
					case 0: //lines
						if (comboBoxDrawMode.SelectedItem.ToString() == Resources.strDrawLines)
						{
							graphics.DrawLine(pen: penDraw, x1: i + 1, y1: panelDraw.Height - array[i], x2: i + 1, y2: panelDraw.Height);
							graphics.DrawLine(pen: penControl, x1: i + 1, y1: 0, x2: i + 1, y2: panelDraw.Height - array[i]);
						}
						else if (comboBoxDrawMode.SelectedItem.ToString() == Resources.strFillRectangles)
						{
							graphics.FillRectangle(brush: brushDraw, x: i + 1, y: panelDraw.Height - array[i], width: i + 1, height: panelDraw.Height);
							graphics.FillRectangle(brush: brushControl, x: i + 1, y: 0, width: i + 1, height: panelDraw.Height - array[i]);
						}
						break;
					case 1: //dotes
						graphics.FillRectangle(brush: brushDraw, x: i + 1, y: panelDraw.Height - array[i], width: 1, height: 1);
						break;
				}
			}
			//RefreshPanel(panelDraw);
		}

		private void DrawArray(byte marker)
		{
			/*bmpSave = new Bitmap(panelDraw.Width, panelDraw.Height);
			graphics = Graphics.FromImage(bmpSave);
			panelDraw.Image = bmpSave;*/
			if (comboBoxVisualizationScheme.SelectedItem.ToString() != Resources.strLines)
			{
				graphics.Clear(panelDraw.BackColor);
			}

			for (byte i = 0; i < array.Length; i++)
			{
				switch (comboBoxVisualizationScheme.SelectedIndex)
				{
					case 0: //lines
						if (radioBoxVisualizationDepthDetailed.Checked && (marker == i))
						{
							if (comboBoxDrawMode.SelectedItem.ToString() == Resources.strDrawLines)
							{
								graphics.DrawLine(pen: penMarker, x1: i + 1, y1: panelDraw.Height - array[i], x2: i + 1, y2: panelDraw.Height);
							}
							else if (comboBoxDrawMode.SelectedItem.ToString() == Resources.strFillRectangles)
							{
								graphics.FillRectangle(brush: brushMarker, x: i + 1, y: panelDraw.Height - array[i], width: i + 1, height: panelDraw.Height);
							}
						}
						else
						{
							if (comboBoxDrawMode.SelectedItem.ToString() == Resources.strDrawLines)
							{
								graphics.DrawLine(pen: penDraw, x1: i + 1, y1: panelDraw.Height - array[i], x2: i + 1, y2: panelDraw.Height);
							}
							else if (comboBoxDrawMode.SelectedItem.ToString() == Resources.strFillRectangles)
							{
								graphics.FillRectangle(brush: brushDraw, x: i + 1, y: panelDraw.Height - array[i], width: i + 1, height: panelDraw.Height);
							}
						}
						if (comboBoxDrawMode.SelectedItem.ToString() == Resources.strDrawLines)
						{
							graphics.DrawLine(pen: penControl, x1: i + 1, y1: 0, x2: i + 1, y2: panelDraw.Height - array[i]);
						}
						else if (comboBoxDrawMode.SelectedItem.ToString() == Resources.strFillRectangles)
						{
							graphics.FillRectangle(brush: brushControl, x: i + 1, y: 0, width: i + 1, height: panelDraw.Height - array[i]);
						}
						break;
					case 1: //dotes
						if (radioBoxVisualizationDepthDetailed.Checked && (marker == i))
						{
							graphics.FillRectangle(brush: brushMarker, x: i + 1, y: panelDraw.Height - array[i], width: 1, height: 1);
						}
						else
						{
							graphics.FillRectangle(brush: brushDraw, x: i + 1, y: panelDraw.Height - array[i], width: 1, height: 1);
						}
						break;
				}
			}
			//RefreshPanel(panelDraw);
		}

		private void Shuffle<T>(T[] array) where T : IComparable
		{
			int n = array.Length;
			while (n > 1)
			{
				int k = rand.Next(n--);
				T temp = array[n];
				array[n] = array[k];
				array[k] = temp;
			}
		}

		private bool IsSorted<T>(T[] array) where T : IComparable
		{
			if (array.Length <= 1)
			{
				return true;
			}

			for (int i = 1; i < array.Length; i++)
			{
				if (array[i].CompareTo(obj: array[i - 1]) < 0)
				{
					return false;
				}
			}
			return true;
		}

		private void Swap(ref byte x, ref byte y)
		{
			swaps++;
			byte temp = x;
			x = y;
			y = temp;
		}

		#region BogoSort

		private void BogoSort()
		{
			while (!IsSorted(array: array))
			{
				Shuffle(array: array);
				if (!radioBoxVisualizationDepthNone.Checked)
				{
					DrawArray();
				}
				MeasureTime();
				ShowProcessingInformation();
				DoLogging();
			}
		}

		#endregion

		#region BozoSort

		private void BozoSort()
		{
			byte i, j;
			while (!IsSorted(array: array))
			{
				i = (byte)rand.Next(maxValue: array.Length);
				j = (byte)rand.Next(maxValue: array.Length);
				Swap(ref array[i], ref array[j]);
				if (radioBoxVisualizationDepthDetailed.Checked)
				{
					DrawArray(marker: j);
					MeasureTime();
					ShowProcessingInformation();
				}
				DoLogging();
				if (radioBoxVisualizationDepthSimple.Checked)
				{
					DrawArray();
					MeasureTime();
					ShowProcessingInformation();
				}
				if (radioBoxVisualizationDepthNone.Checked)
				{
					graphics.Clear(color: panelDraw.BackColor);
					//RefreshPanel(panelDraw);
					MeasureTime();
					ShowProcessingInformation();
				}
			}
		}

		#endregion

		#region RandomSort

		private void RandomSort()
		{
			byte i, j;
			while (!IsSorted(array: array))
			{
				i = (byte)rand.Next(maxValue: array.Length);
				j = (byte)rand.Next(maxValue: array.Length);
				comparisons++;
				if (array[i] < array[j])
				{
					Swap(x: ref array[i], y: ref array[j]);
					if (radioBoxVisualizationDepthDetailed.Checked)
					{
						DrawArray(marker: j);
						MeasureTime();
						ShowProcessingInformation();
					}
					DoLogging();
				}
				if (radioBoxVisualizationDepthSimple.Checked)
				{
					DrawArray();
					MeasureTime();
					ShowProcessingInformation();
				}
				if (radioBoxVisualizationDepthNone.Checked)
				{
					graphics.Clear(color: panelDraw.BackColor);
					//RefreshPanel(panelDraw);
					MeasureTime();
					ShowProcessingInformation();
				}
			}
		}

		#endregion

		#region TrippelSort

		private void TrippelSort1(byte l, byte r)
		{
			byte k;
			comparisons++;
			if (array[l] > array[r])
			{
				Swap(x: ref array[l], y: ref array[r]);
				if (radioBoxVisualizationDepthDetailed.Checked)
				{
					DrawArray(marker: (byte)(r + 1));
					MeasureTime();
					ShowProcessingInformation();
				}
				DoLogging();
			}
			if (radioBoxVisualizationDepthSimple.Checked)
			{
				DrawArray();
				MeasureTime();
				ShowProcessingInformation();
			}
			if (l < r - 1)
			{
				k = (byte)((r - l + 1) / 3);
				TrippelSort1(l: l, r: (byte)(r - k));
				TrippelSort1(l: (byte)(l + k), r: r);
				TrippelSort1(l: l, r: (byte)(r - k));
			}
			if (radioBoxVisualizationDepthNone.Checked)
			{
				graphics.Clear(color: panelDraw.BackColor);
				//RefreshPanel(panelDraw);
				MeasureTime();
				ShowProcessingInformation();
			}
		}

		private void TrippelSort2(byte l, byte r)
		{
			byte k;
			comparisons++;
			if (radioBoxVisualizationDepthSimple.Checked)
			{
				DrawArray();
				MeasureTime();
				ShowProcessingInformation();
			}
			if (l < r - 1)
			{
				k = (byte)((r - l + 1) / 3);
				TrippelSort2(l: l, r: (byte)(r - k));
				TrippelSort2(l: (byte)(l + k), r: r);
				TrippelSort2(l: l, r: (byte)(r - k));
			}
			else if (array[l] > array[r])
			{
				Swap(x: ref array[l], y: ref array[r]);
				if (radioBoxVisualizationDepthDetailed.Checked)
				{
					DrawArray(marker: (byte)(r + 1));
					MeasureTime();
					ShowProcessingInformation();
				}
				DoLogging();
			}
			if (radioBoxVisualizationDepthNone.Checked)
			{
				graphics.Clear(color: panelDraw.BackColor);
				//RefreshPanel(panelDraw);
				MeasureTime();
				ShowProcessingInformation();
			}
		}

		#endregion

		#region BubbleSort

		private void BubbleSort1()
		{
			for (byte i = 1; i <= array.Length - 1; i++)
			{
				for (byte j = 0; j < array.Length - i; j++)
				{
					comparisons++;
					if (array[j] > array[j + 1])
					{
						Swap(x: ref array[j], y: ref array[j + 1]);
						if (radioBoxVisualizationDepthDetailed.Checked)
						{
							DrawArray(marker: (byte)(j + 1));
							MeasureTime();
							ShowProcessingInformation();
						}
						DoLogging();
					}
				}
				if (radioBoxVisualizationDepthSimple.Checked)
				{
					DrawArray();
					MeasureTime();
					ShowProcessingInformation();
				}
			}
			if (radioBoxVisualizationDepthNone.Checked)
			{
				graphics.Clear(color: panelDraw.BackColor);
				//RefreshPanel(panelDraw);
				MeasureTime();
				ShowProcessingInformation();
			}
		}

		private void BubbleSort2()
		{
			byte n = (byte)array.Length;
			bool flipped;
			do
			{
				flipped = false;
				for (byte i = 0; i < n - 1; i++)
				{
					comparisons++;
					if (array[i] > array[i + 1])
					{
						Swap(x: ref array[i], y: ref array[i + 1]);
						flipped = true;
						if (radioBoxVisualizationDepthDetailed.Checked)
						{
							DrawArray(marker: (byte)(i + 1));
							MeasureTime();
							ShowProcessingInformation();
						}
						DoLogging();
					}
				}
				n--;
				if (radioBoxVisualizationDepthSimple.Checked)
				{
					DrawArray();
					MeasureTime();
					ShowProcessingInformation();
				}
			} while (flipped);
			if (radioBoxVisualizationDepthNone.Checked)
			{
				graphics.Clear(color: panelDraw.BackColor);
				//RefreshPanel(panelDraw);
				MeasureTime();
				ShowProcessingInformation();
			}
		}

		private void BubbleSort3()
		{
			byte n = (byte)array.Length;
			do
			{
				byte newn = 1;
				for (byte i = 0; i < n - 1; i++)
				{
					comparisons++;
					if (array[i] > array[i + 1])
					{
						newn = (byte)(i + 1);
						Swap(x: ref array[i], y: ref array[i + 1]);
						if (radioBoxVisualizationDepthDetailed.Checked)
						{
							DrawArray(marker: (byte)(i + 1));
							MeasureTime();
							ShowProcessingInformation();
						}
						DoLogging();
					}
				}
				n = newn;
				if (radioBoxVisualizationDepthSimple.Checked)
				{
					DrawArray();
					MeasureTime();
					ShowProcessingInformation();
				}
			} while (n > 1);
			if (radioBoxVisualizationDepthNone.Checked)
			{
				graphics.Clear(color: panelDraw.BackColor);
				//RefreshPanel(panelDraw);
				MeasureTime();
				ShowProcessingInformation();
			}
		}

		#endregion

		#region Statusbar

		private void SetStatusbar(object sender, EventArgs e)
		{
			if (sender is Button button)
			{
				toolStripStatusLabel.Text = button.AccessibleDescription;
			}
			else if (sender is Label label)
			{
				toolStripStatusLabel.Text = label.AccessibleDescription;
			}
			else if (sender is ComboBox comboBox)
			{
				toolStripStatusLabel.Text = comboBox.AccessibleDescription;
			}
			else if (sender is RadioButton radioButton)
			{
				toolStripStatusLabel.Text = radioButton.AccessibleDescription;
			}
			else if (sender is CheckBox checkBox)
			{
				toolStripStatusLabel.Text = checkBox.AccessibleDescription;
			}
			else if (sender is PictureBox pictureBox)
			{
				toolStripStatusLabel.Text = pictureBox.AccessibleDescription;
			}
			else if (sender is StatusStrip statusStrip)
			{
				toolStripStatusLabel.Text = statusStrip.AccessibleDescription;
			}
			else if (sender is GroupBox groupBox)
			{
				toolStripStatusLabel.Text = groupBox.AccessibleDescription;
			}
			else if (sender is TableLayoutPanel tableLayoutPanel)
			{
				toolStripStatusLabel.Text = tableLayoutPanel.AccessibleDescription;
			}
			else if (sender is ToolStripStatusLabel toolStripStatusLabel)
			{
				toolStripStatusLabel.Text = toolStripStatusLabel.AccessibleDescription;
			}
		}

		private void ClearStatusbar(object sender, EventArgs e)
		{
			toolStripStatusLabel.Text = "";
		}

		#endregion

		#region Mainform-Events

		private void MainForm_Load(object sender, EventArgs e)
		{
			DoubleBuffered = true;
			SetStyle(ControlStyles.OptimizedDoubleBuffer |
				ControlStyles.UserPaint |
				ControlStyles.AllPaintingInWmPaint, true);
			UpdateStyles();
			typeof(Panel).InvokeMember(name: "DoubleBuffered", invokeAttr: BindingFlags.SetProperty |
				BindingFlags.Instance |
				BindingFlags.NonPublic, binder: null, target: panelDraw, args: new object[] { true });
			ClearStatusbar(sender: null, e: null);
			graphics = panelDraw.CreateGraphics();
			comboBoxSortingAlgorithm.Items.AddRange(items: new object[] {
				Resources.strBogoSort,
				Resources.strBozoSort,
				Resources.strRandomSort,
				Resources.strTrippelSort1,
				Resources.strTrippelSort2,
				Resources.strBubbleSort1,
				Resources.strBubbleSort2,
				Resources.strBubbleSort3});
			comboBoxSortingAlgorithm.SelectedIndex = 5;
			comboBoxVisualizationScheme.Items.AddRange(items: new object[] {
				Resources.strLines,
				Resources.strDotes});
			comboBoxVisualizationScheme.SelectedIndex = 0;
			comboBoxShuffleMode.Items.AddRange(items: new object[] {
				Resources.strRandom,
				Resources.strSortedForward,
				Resources.strSortedReverse});
			comboBoxShuffleMode.SelectedIndex = 0;
			comboBoxDrawMode.Items.AddRange(items: new object[] {
				Resources.strDrawLines,
				Resources.strFillRectangles});
			comboBoxDrawMode.SelectedIndex = 0;
			InitArray();
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (thread != null)
			{
				thread.Abort();
				thread.Join();
			}
		}

		#endregion

		#region Button-Events

		private void ButtonShuffle_Click(object sender, EventArgs e)
		{
			if (!isShuffled)
			{
				isShuffled = true;
			}
			labelComparisonValue.Text =
				labelSwapValue.Text =
				labelRuntimeValue.Text =
				labelCsRelationValue.Text = Resources.strNumberZero;
			switch (comboBoxShuffleMode.SelectedIndex)
			{
				case 0: //random
					Shuffle(array: array);
					break;
				case 1: //sorted forward
					InitArray();
					break;
				case 2: //sorted reverse
					InitArrayReverse();
					break;
				default:
					Shuffle(array: array);
					break;
			}
			graphics.Clear(color: panelDraw.BackColor);
			DrawArray();
		}

		private void ButtonSort_Click(object sender, EventArgs e)
		{
			if (!isShuffled)
			{
				labelComparisonValue.Text =
					labelSwapValue.Text =
					labelRuntimeValue.Text =
					labelCsRelationValue.Text = Resources.strNumberZero;
				isShuffled = true;
				Shuffle(array: array);
				DrawArray();
			}
			MeasureTime();
			if (thread != null)
			{
				thread.Abort();
				thread.Join();
			}
			void threadStart()
			{
				watch.Reset();
				watch.Start();
				comboBoxSortingAlgorithm.Enabled = false;
				checkBoxEnableLogging.Enabled = false;
				buttonSaveLogging.Enabled = false;
				buttonShuffle.Enabled = false;
				buttonSort.Enabled = false;
				dicLogging.Clear();
				comparisons = 0;
				swaps = 0;
				switch (comboBoxSortingAlgorithm.SelectedIndex)
				{
					case 0: //BogoSort = Monkey Sort, Stupid Sort
						BogoSort();
						break;
					case 1: //Bozo Sort
						BozoSort();
						break;
					case 2: //Random Sort
						RandomSort();
						break;
					case 3: //Trippel Sort = Stooge Sort (original version)
						TrippelSort1(l: 0, r: (byte)(array.Length - 1));
						break;
					case 4: //Trippel Sort = Stooge Sort (comparative reduction)
						TrippelSort2(l: 0, r: (byte)(array.Length - 1));
						break;
					case 5: //Bubble Sort (original version)
						BubbleSort1();
						break;
					case 6: //Bubble Sort (premature termination)
						BubbleSort2();
						break;
					case 7: //Bubble Sort (comparative reduction)
						BubbleSort3();
						break;
				}
				if (checkBoxFinalEvent.Checked)
				{
					ApplyFinalEvent();
				}

				comboBoxSortingAlgorithm.Enabled = true;
				checkBoxEnableLogging.Enabled = true;
				buttonShuffle.Enabled = true;
				buttonSort.Enabled = true;
				if (checkBoxEnableLogging.Checked)
				{
					buttonSaveLogging.Enabled = true;
				}
				watch.Stop();
			}
			thread = new Thread(threadStart);
			thread.Start();
			MeasureTime();
		}

		private void ButtonSaveLogging_Click(object sender, EventArgs e)
		{
			if (saveFileDialog.ShowDialog() == DialogResult.OK)
			{
				StreamWriter stream = File.CreateText(path: saveFileDialog.FileName);
				for (int i = 0; i < dicLogging.Count; i++)
				//for (int i = dicLogging.Count - 1; i >= 0; i--)
				{
					/*var item = dicLogging.ElementAt(i);
					var itemKey = dicLogging.ElementAt(i).Key;
					var itemValue = dicLogging.ElementAt(i).Value;*/
					stream.WriteLine(value: i.ToString() + ";" + dicLogging.ElementAt(i).Key + ";" + dicLogging.ElementAt(i).Value + ";");
				}
				/*
				uint i = 0;
				foreach(var pair in dicLogging)
				{
					i++;
					stream.WriteLine(i.ToString() + ";" + pair.Key + ";" + pair.Value + ";");
				}
				*/
			}
		}

		#endregion
	}
}