﻿using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using System.ComponentModel;
using System.Windows.Controls;
using System;
using System.Windows.Media;
using System.IO;
using System.Windows.Documents;
using System.Collections.Generic;
using System.Xml;
using System.Windows.Controls.Primitives;

namespace JocysCom.ClassLibrary.Controls
{
	public partial class ControlsHelper
	{
		public static void EnableWithDelay(UIElement control)
		{
			Task.Run(async delegate
			{
				// Logical delay without blocking the current hardware thread.
				await Task.Delay(500).ConfigureAwait(true);
				control.Dispatcher.Invoke(() => control.IsEnabled = true);
			});
		}

		private static bool? _IsDesignModeWPF;

		public static bool IsDesignMode(UIElement component)
		{
			if (!_IsDesignModeWPF.HasValue)
				_IsDesignModeWPF = IsDesignMode1(component);
			return _IsDesignModeWPF.Value;
		}

		private static bool IsDesignMode1(UIElement component)
		{
			// Check 1.
			if (DesignerProperties.GetIsInDesignMode(component))
				return true;
			//If WPF hosted in WinForms.
			var ea = System.Reflection.Assembly.GetEntryAssembly();
			if (ea != null && ea.Location.Contains("VisualStudio"))
				return true;
			//If WPF hosted in WinForms.
			ea = System.Reflection.Assembly.GetExecutingAssembly();
			if (ea != null && ea.Location.Contains("VisualStudio"))
				return true;
			return false;
		}

		public static T Clone<T>(T o)
		{
			var sb = new System.Text.StringBuilder();
			var writer = XmlWriter.Create(sb, new XmlWriterSettings
			{
				Indent = true,
				ConformanceLevel = ConformanceLevel.Fragment,
				OmitXmlDeclaration = true,
				NamespaceHandling = NamespaceHandling.OmitDuplicates,
				// XmlReader normalizes all newlines and converts '\r\n' to '\n'.
				// This requires to save NewLines with  option which
				// "Entitize" option replace '\r' with '&#xD;' in text node values.
				NewLineHandling = NewLineHandling.Entitize,
			});
			var manager = new System.Windows.Markup.XamlDesignerSerializationManager(writer);
			manager.XamlWriterMode = System.Windows.Markup.XamlWriterMode.Expression;
			System.Windows.Markup.XamlWriter.Save(o, manager);
			var stringReader = new StringReader(sb.ToString());
			var xmlReader = XmlReader.Create(stringReader);
			var item = System.Windows.Markup.XamlReader.Load(xmlReader);
			if (item == null)
				throw new ArgumentNullException("Could not be cloned.");
			return (T)item;
		}

		/// <summary>
		/// Change value if it is different only.
		/// This helps not to trigger control events when doing frequent events.
		/// </summary>
		public static void SetText(Label control, string format, params object[] args)
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));
			var text = (args == null)
				? format
				: string.Format(format, args);
			if (control.Content as string != text)
				control.Content = text;
		}


		/// <summary>
		/// Change value if it is different only.
		/// This helps not to trigger control events when doing frequent events.
		/// </summary>
		public static void SetText(GroupBox control, string format, params object[] args)
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));
			var text = (args == null)
				? format
				: string.Format(format, args);
			if (control.Header as string != text)
				control.Header = text;
		}

		/// <summary>
		/// Change value if it is different only.
		/// This helps not to trigger control events when doing frequent events.
		/// </summary>
		public static void SetText(TextBox control, string format, params object[] args)
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));
			var text = (args == null)
				? format ?? ""
				: string.Format(format ?? "", args);
			if (control.Text != text)
				control.Text = text;
		}

		/// <summary>
		/// Change value if it is different only.
		/// This helps not to trigger control events when doing frequent events.
		/// </summary>
		public static void SetText(TextBlock control, string format, params object[] args)
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));
			var text = (args == null)
				? format ?? ""
				: string.Format(format ?? "", args);
			if (control.Text != text)
				control.Text = text;
		}

		public static void SetTextFromResource(RichTextBox box, byte[] rtf)
		{
			var ms = new MemoryStream(rtf);
			var textRange = new TextRange(box.Document.ContentStart, box.Document.ContentEnd);
			textRange.Load(ms, DataFormats.Rtf);
			ms.Dispose();
			box.Document.PagePadding = new Thickness(8);
			box.IsDocumentEnabled = true;
			HookHyperlinks(box, null);
		}

		/// <summary>
		/// Change value if it is different only.
		/// This helps not to trigger control events when doing frequent events.
		/// </summary>
		public static void SetChecked(System.Windows.Controls.Primitives.ToggleButton control, bool check)
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));
			if (control.IsChecked != check)
				control.IsChecked = check;
		}

		/// <summary>
		/// Change value if it is different only.
		/// This helps not to trigger control events when doing frequent events.
		public static void SetEnabled(UIElement control, bool enabled)
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));
			if (control.IsEnabled != enabled)
				control.IsEnabled = enabled;
		}

		/// <summary>
		/// Change value if it is different only.
		/// This helps not to trigger control events when doing frequent events.
		public static void SetVisible(UIElement control, bool enabled)
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));
			var visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
			if (control.Visibility != visibility)
				control.Visibility = visibility;
		}

		public static void SetItemsSource(ItemsControl grid, IBindingList list)
		{
			if (list == null)
			{
				if (grid.ItemsSource is System.Windows.Data.BindingListCollectionView view)
				{
					grid.ItemsSource = null;
					view.DetachFromSourceCollection();
				}
				return;
			}
			var currentView = (System.Windows.Data.BindingListCollectionView)grid.ItemsSource;
			// If same list then...
			if (currentView?.SourceCollection == list)
				return;
			var newView = new System.Windows.Data.BindingListCollectionView(list);
			// Clear Items to avoid exception: "Items collection must be empty before using ItemsSource"
			grid.Items.Clear();
			grid.ItemsSource = newView;
		}

		private static void HookHyperlinks(object sender, TextChangedEventArgs e)
		{
			var doc = (sender as RichTextBox).Document;
			for (var position = doc.ContentStart;
				position != null && position.CompareTo(doc.ContentEnd) <= 0;
				position = position.GetNextContextPosition(LogicalDirection.Forward))
			{
				if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.ElementEnd)
				{
					if (position.Parent is Hyperlink link)
						link.RequestNavigate += link_RequestNavigate;
					else if (position.Parent is Span span)
					{
						var range = new TextRange(span.ContentStart, span.ContentEnd);
						if (Uri.TryCreate(range.Text, UriKind.Absolute, out var uriResult))
						{
							if (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)
							{
								var h = new Hyperlink(range.Start, range.End);
								h.RequestNavigate += link_RequestNavigate;
								h.NavigateUri = new Uri(range.Text);
								h.Cursor = System.Windows.Input.Cursors.Hand;
							}
						}
					}
				}
			}
		}
		private static void link_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
		{
			var link = (Hyperlink)sender;
			OpenUrl(link.NavigateUri.AbsoluteUri);
			e.Handled = true;
		}

		#region IsVisibleToUser

		public static Point[] GetPoints(Control control, bool relative = false)
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));
			var pos = relative
				? new Point(0, 0)
				// Get control position on the screen
				: control.PointToScreen(new Point(0, 0));
			var pointsToCheck =
				new Point[]
					{
						// Top-Left.
						pos,
						// Top-Right.
						new Point(pos.X + control.ActualWidth - 1, pos.Y),
						// Bottom-Left.
						new Point(pos.X, pos.Y + control.ActualHeight - 1),
						// Bottom-Right.
						new Point(pos.X + control.ActualWidth - 1, pos.Y + control.ActualHeight - 1),
						// Middle-Centre.
						new Point(pos.X + control.ActualWidth/2, pos.Y + control.ActualHeight/2)
					};
			return pointsToCheck;
		}

		/*
		public static bool IsControlVisibleToUser(Control control)
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));
			var handle = (PresentationSource.FromVisual(control) as System.Windows.Interop.HwndSource)?.Handle;
			if (!handle.HasValue)
				return false;
			var children = GetAll<DependencyObject>(control, true);
			// Return true if any of the controls is visible.
			var pointsToCheck = GetPoints(control, true);
			foreach (var p in pointsToCheck)
			{
				//var hwnd = NativeMethods.WindowFromPoint(p);
				//if (hwnd == IntPtr.Zero)
				//	continue;
				var result = VisualTreeHelper.HitTest(control, p);
				if (result == null)
					continue;
				if (children.Contains(result.VisualHit))
					return true;
				//var other = Control.FromChildHandle(hwnd);
				//if (other == null)
				//	continue;
				//if (GetAll(control, null, true).Contains(other))
			}
			return false;
		}
		*/
		/// <summary>
		/// Get parent control of specific type.
		/// </summary>
		public static T GetParent<T>(DependencyObject control, bool includeTop = false) where T : class
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));
			var parent = control;
			while (parent != null)
			{
				if (parent is T && (includeTop || parent != control))
					return (T)(object)parent;
				var p = VisualTreeHelper.GetParent(parent);
				if (p == null)
					p = LogicalTreeHelper.GetParent(parent);
				parent = p;
			}
			return null;
		}

		public static void AddWeakHandlerOnWindowClosing(DependencyObject control, EventHandler<CancelEventArgs> handler)
		{
			var w = GetParent<Window>(control);
			if (w == null)
				return;
			WeakEventManager<Window, CancelEventArgs>.AddHandler(w, nameof(Window.Closing), handler);
		}

		/// <summary>
		/// Get all child controls with path.
		/// Use regex to make shorter tabbed path:
		/// var rx = new Regex("[^.]+[.]+");
		/// var tabbedPath = rx.Replace(item.Path, "\t");
		/// </summary>
		public static Dictionary<string, DependencyObject> GetAll(string path, DependencyObject control, Type type = null, bool includeTop = false)
		{
			if (control == null)
				throw new ArgumentNullException(nameof(control));
			// Create new list.
			var controls = new Dictionary<string, DependencyObject>();
			if (string.IsNullOrEmpty(path))
				path = $"{control.GetType().Name} {(control as FrameworkElement)?.Name}".TrimEnd();
			// Add top control if required.
			if (includeTop && !controls.Values.Contains(control))
			{
				controls.Add(path, control);
			}
			// If control is Visual then then...
			if (control is Visual || control is System.Windows.Media.Media3D.Visual3D)
			{
				var childrenCount = VisualTreeHelper.GetChildrenCount(control);
				for (int i = 0; i < childrenCount; i++)
				{
					var child = VisualTreeHelper.GetChild(control, i);
					var childKey = $"{path}[{i}].{child.GetType().Name} {(child as FrameworkElement)?.Name}".TrimEnd();
					//controls.Add(childKey, child);
					// Get children of children.
					var pairs = GetAll(childKey, child, null, true);
					foreach (var pair in pairs)
					{
						if (!controls.ContainsValue(pair.Value))
							controls.Add(pair.Key, pair.Value);
					}
				}
			}
			// If contorl is FrameworkElement then...
			if (control is FrameworkElement || control is FrameworkContentElement)
			{
				var logicalChildren = LogicalTreeHelper.GetChildren(control).OfType<DependencyObject>().ToList();
				for (int i = 0; i < logicalChildren.Count; i++)
				{
					var child = logicalChildren[i];
					var childKey = $"{path}[{i}].{child.GetType().Name} {(child as FrameworkElement)?.Name}".TrimEnd();
					//controls.Add(childKey, child);
					// Get children of children.
					var pairs = GetAll(childKey, child, null, true);
					foreach (var pair in pairs)
					{
						if (!controls.ContainsValue(pair.Value))
							controls.Add(pair.Key, pair.Value);
					}
				}
			}
			// If type is set then...
			if (type != null)
			{
				var keys = controls.Where(x => type.IsInterface ? x.Value.GetType().GetInterfaces().Contains(type) : type.IsAssignableFrom(x.Value.GetType()));
				foreach (var pair in keys)
				{
					if (controls.ContainsKey(pair.Key))
						controls.Remove(pair.Key);
				}
			}
			return controls;
		}

		/// <summary>
		/// Get all child controls.
		/// </summary>
		public static IEnumerable<DependencyObject> GetAll(DependencyObject control, Type type = null, bool includeTop = false)
		{
			return GetAll(null, control, type, includeTop).Values.ToList();
		}

		/// <summary>
		/// Get all child controls.
		/// </summary>
		public static T[] GetAll<T>(Control control, bool includeTop = false)
		{
			if (control == null)
				return new T[0];
			return GetAll(control, typeof(T), includeTop).Cast<T>().ToArray();
		}

		public static void GetActiveControl(FrameworkElement control, out FrameworkElement activeControl, out string activePath)
		{
			string _activePath = null;
			Invoke(() =>
			{
				_activePath = string.Format("/{0}", control?.Name);
			});
			activePath = _activePath;
			// Return current control by default.
			activeControl = control;
			// If control can contains active controls.
			var container = control as DependencyObject;
			while (container != null)
			{
				control = System.Windows.Input.FocusManager.GetFocusedElement(control) as FrameworkElement;
				if (control == null)
					break;
				Invoke(() =>
				{
					_activePath = string.Format("/{0}", control?.Name);
				});

				activePath += _activePath;
				activeControl = control;
				container = control;
			}
		}

		#endregion

		#region Apply Grid Border Style

		public static void ApplyBorderStyle(DataGrid grid)
		{
			if (grid == null)
				throw new ArgumentNullException(nameof(grid));
			grid.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
			//grid.BorderThickness = BorderStyle.None;
			//grid.EnableHeadersVisualStyles = false;
			//grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
			//grid.ColumnHeadersDefaultCellStyle.BackColor = SystemColors.Control;
			//grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
			//grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
			//grid.RowHeadersDefaultCellStyle.BackColor = SystemColors.Control;
			//grid.BackColor = SystemColors.Window;
			//grid.DefaultCellStyle.BackColor = SystemColors.Window;
			//grid.CellPainting += Grid_CellPainting;
			//grid.SelectionChanged += Grid_SelectionChanged;
			//grid.CellFormatting += Grid_CellFormatting;
			//if (updateEnabledProperty)
			//	grid.CellClick += Grid_CellClick;
		}

		/*
		private static void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;
			var grid = (DataGridView)sender;
			// If add new record row.
			if (grid.AllowUserToAddRows && e.RowIndex + 1 == grid.Rows.Count)
				return;
			var column = grid.Columns[e.ColumnIndex];
			var item = grid.Rows[e.RowIndex].DataBoundItem;
			if (column.DataPropertyName == "Enabled" || column.DataPropertyName == "IsEnabled")
			{
				SetEnabled(item, !GetEnabled(item));
				grid.Invalidate();
			}
		}

		private static void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
		{
			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;
			var grid = (DataGridView)sender;
			// If add new record row.
			if (grid.AllowUserToAddRows && e.RowIndex + 1 == grid.Rows.Count)
				return;
			var row = grid.Rows[e.RowIndex];
			if (e.RowIndex > -1 && e.ColumnIndex > -1)
			{
				var item = row.DataBoundItem;
				// If grid is virtual then...
				if (item == null)
				{
					var list = grid.DataSource as IBindingList;
					if (list != null)
						item = list[e.RowIndex];
				}
				var enabled = true;
				if (item != null)
					enabled = GetEnabled(item);
				var fore = enabled ? grid.DefaultCellStyle.ForeColor : SystemColors.ControlDark;
				var selectedBack = enabled ? grid.DefaultCellStyle.SelectionBackColor : SystemColors.ControlDark;
				// Apply style to row header.
				if (row.HeaderCell.Style.ForeColor != fore)
					row.HeaderCell.Style.ForeColor = fore;
				if (row.HeaderCell.Style.SelectionBackColor != selectedBack)
					row.HeaderCell.Style.SelectionBackColor = selectedBack;
				// Apply style to cell
				var cell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
				if (cell.Style.ForeColor != fore)
					cell.Style.ForeColor = fore;
				if (cell.Style.SelectionBackColor != selectedBack)
					cell.Style.SelectionBackColor = selectedBack;
			}
		}

		private static void Grid_SelectionChanged(object sender, EventArgs e)
		{
			// Sort issue with paint artifcats.
			var grid = (DataGridView)sender;
			grid.Invalidate();
		}

		private static void SetEnabled(object item, bool enabled)
		{
			var enabledProperty = item.GetType().GetProperties().FirstOrDefault(x => x.Name == "Enabled" || x.Name == "IsEnabled");
			if (enabledProperty != null)
			{
				enabledProperty.SetValue(item, enabled, null);
			}
		}

		private static bool GetEnabled(object item)
		{
			var enabledProperty = item.GetType().GetProperties().FirstOrDefault(x => x.Name == "Enabled" || x.Name == "IsEnabled");
			var enabled = enabledProperty == null ? true : (bool)enabledProperty.GetValue(item, null);
			return enabled;
		}

		private static void Grid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
		{
			// Header and cell borders must be set to "Single" style.
			var grid = (DataGridView)sender;
			var firstVisibleColumn = grid.Columns.Cast<DataGridViewColumn>().Where(x => x.Displayed).Min(x => x.Index);
			var lastVisibleColumn = grid.Columns.Cast<DataGridViewColumn>().Where(x => x.Displayed).Max(x => x.Index);
			var selected = e.RowIndex > -1 ? grid.Rows[e.RowIndex].Selected : false;
			e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.Border);
			var bounds = e.CellBounds;
			var tl = new Point(bounds.X, bounds.Y);
			var tr = new Point(bounds.X + bounds.Width - 1, bounds.Y);
			var bl = new Point(bounds.X, bounds.Y + bounds.Height - 1);
			var br = new Point(bounds.X + bounds.Width - 1, bounds.Y + bounds.Height - 1);
			Color backColor;
			// If top left corner and column header then...
			if (e.RowIndex == -1)
			{
				backColor = selected
					? grid.ColumnHeadersDefaultCellStyle.SelectionBackColor
					: grid.ColumnHeadersDefaultCellStyle.BackColor;
			}
			// If row header then...
			else if (e.ColumnIndex == -1 && e.RowIndex > -1)
			{
				var row = grid.Rows[e.RowIndex];
				backColor = selected
					? row.HeaderCell.Style.SelectionBackColor
					: grid.RowHeadersDefaultCellStyle.BackColor;
			}
			// If normal cell then...
			else
			{
				var row = grid.Rows[e.RowIndex];
				var cell = row.Cells[e.ColumnIndex];
				backColor = selected
					? cell.InheritedStyle.SelectionBackColor
					: cell.InheritedStyle.BackColor;
			}
			// Cell background colour.
			var back = new Pen(backColor, 1);
			// Border colour.
			var border = new Pen(SystemColors.Control, 1);
			// Do not draw borders for selected device.
			Pen c;
			// Top
			e.Graphics.DrawLine(back, tl, tr);
			// Left (only if not first)
			c = !selected && e.ColumnIndex > firstVisibleColumn ? border : back;
			e.Graphics.DrawLine(c, bl, tl);
			// Right (always)
			c = back;
			e.Graphics.DrawLine(c, tr, br);
			// Bottom (always)
			c = border;
			e.Graphics.DrawLine(c, bl, br);
			back.Dispose();
			border.Dispose();
			e.Handled = true;
		}

		*/

		#endregion

		#region Data Grid Functions

		/// <summary>
		/// Get list of primary keys of items selected in the grid.
		/// </summary>
		/// <typeparam name="T">Type of Primary key.</typeparam>
		/// <param name="grid">Grid for getting selection</param>
		/// <param name="primaryKeyPropertyName">Primary key name.</param>
		public static List<T> GetSelection<T>(DataGrid grid, string keyPropertyName = null)
		{
			if (grid == null)
				throw new ArgumentNullException(nameof(grid));
			var list = new List<T>();
			var items = grid.SelectedItems.Cast<object>().ToArray();
			// If nothing selected then try to get rows from cells.
			if (items.Length == 0)
				items = grid.SelectedCells.Cast<DataGridCellInfo>().Select(x => x.Item).Distinct().ToArray();
			// If nothing selected then return.
			if (items.Length == 0)
				return list;
			var pi = GetPropertyInfo(keyPropertyName, items[0]);
			for (var i = 0; i < items.Length; i++)
			{
				var value = GetValue<T>(items[i], keyPropertyName, pi);
				list.Add(value);
			}
			return list;
		}

		public static void RestoreSelection<T>(DataGrid grid, string keyPropertyName, List<T> list, bool selectFirst = true)
		{
			if (grid == null)
				throw new ArgumentNullException(nameof(grid));
			if (list == null)
				throw new ArgumentNullException(nameof(list));
			var items = grid.Items.Cast<object>().ToArray();
			// Return if grid is empty.
			if (items.Length == 0)
				return;
			// If something to restore then...
			if (list.Count > 0)
			{
				var selectedItems = new List<object>();
				var pi = GetPropertyInfo(keyPropertyName, items[0]);
				for (var i = 0; i < items.Length; i++)
				{
					var item = items[i];
					var val = GetValue<T>(item, keyPropertyName, pi);
					if (list.Contains(val))
						selectedItems.Add(item);
				}
				if (grid.SelectionMode == DataGridSelectionMode.Single)
				{
					grid.SelectedItem = selectedItems.FirstOrDefault();
				}
				else
				{
					// Remove items which should not be selected.
					var itemsToUnselect = grid.SelectedItems.Cast<object>().Except(selectedItems);
					foreach (var item in itemsToUnselect)
						grid.SelectedItems.Remove(item);
					var itemsToSelect = selectedItems.Except(grid.SelectedItems.Cast<object>());
					foreach (var item in itemsToSelect)
						grid.SelectedItems.Add(item);
				}
			}
			// If must select first row and nothing is selected then...
			if (selectFirst && grid.SelectedItems.Count == 0)
				grid.SelectedItem = items[0];
		}

		#endregion

		#region TextBoxBase

		public static VerticalAlignment GetScrollVerticalAlignment(ScrollViewer control)
		{
			// Vertical scroll position.
			var offset = control.VerticalOffset;
			// Vertical size of the scrollable content area.
			var height = control.ExtentHeight;
			// Vertical size of the visible content area.
			var visibleView = control.ViewportHeight;
			//var scrollBarHeight = control.ActualHeight - control.ViewportHeight;
			// Allow flexibility of 2 pixels.
			var flex = 2;
			if (height - offset - visibleView < flex)
				return VerticalAlignment.Bottom;
			if (offset < flex)
				return VerticalAlignment.Top;
			return VerticalAlignment.Center;
		}

		public static void AutoScroll(Control control)
		{
			ScrollViewer sv = null;
			if (!(control is ScrollViewer))
			{
				var all = GetAll<ScrollViewer>(control);
				// Try to get one with visible vertical bar first otherwise get default.
				sv = all
					.Where(x => x.ComputedVerticalScrollBarVisibility == Visibility.Visible)
					.FirstOrDefault() ?? all.FirstOrDefault();
			}
			//if (control is TextBoxBase tb)
			//{
			//	var border = (Border)VisualTreeHelper.GetChild(control, 0);
			//	sv = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
			//}
			if (sv != null)
			{
				var scrollPosition = GetScrollVerticalAlignment(sv);
				if (scrollPosition == VerticalAlignment.Bottom && control.IsVisible)
					sv.ScrollToEnd();
			}
		}

		//public static void Measure(Control control)
		//{
		//	var available = LayoutInformation.GetLayoutSlot(control);
		//	Size s = new Size(available.Width, available.Height);
		//	control.Measure(s);
		//	control.Arrange(available);
		//}

		public static void EnableAutoScroll(TextBoxBase control, bool enable = true)
		{
			control.TextChanged -= TextBoxBase_TextChanged;
			control.IsVisibleChanged -= TextBoxBase_IsVisibleChanged;
			control.Unloaded -= TextBoxBase_Unloaded;
			if (enable)
			{
				control.TextChanged += TextBoxBase_TextChanged;
				control.IsVisibleChanged += TextBoxBase_IsVisibleChanged;
				control.Unloaded += TextBoxBase_Unloaded;
			}
		}

		private static void TextBoxBase_Unloaded(object sender, RoutedEventArgs e)
			=> EnableAutoScroll((TextBox)sender, false);

		private static void TextBoxBase_TextChanged(object sender, TextChangedEventArgs e)
			=> AutoScroll((TextBox)sender);

		private static void TextBoxBase_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
			=> AutoScroll((TextBox)sender);

		#endregion

		// Contains unique list of control IDs for the applicaiton.
		private static SortedSet<int> LoadedControls = new SortedSet<int>();

		/// <summary>
		/// Returnd false if displayed in desing mode (IDE).
		/// Return true if control is not in the list of loaded controls.
		/// Add control to the list of loaded controls.
		/// IMPORTANT! Must be used in pair with AllowUnload.
		/// </summary>
		public static bool AllowLoad(FrameworkElement control)
		{
			if (IsDesignMode(control))
				return false;
			var code = control.GetHashCode();
			return LoadedControls.Add(code);
		}

		/// <summary>
		/// Returnd false if displayed in desing mode (IDE).
		/// Return true if control is in the list of loaded controls.
		/// Remove control from the list of loaded controls.
		/// IMPORTANT! Must be used in pair with AllowLoad.
		/// </summary>
		public static bool AllowUnload(FrameworkElement control)
		{
			if (IsDesignMode(control))
				return false;
			var code = control.GetHashCode();
			return LoadedControls.Remove(code);
		}


		/// <summary>
		/// Returnd false if displayed in desing mode (IDE).
		/// Return true if control is in the list of loaded controls.
		/// Remove control from the list of loaded controls.
		/// </summary>
		public static bool IsLoaded(FrameworkElement control)
		{
			if (IsDesignMode(control))
				return false;
			var code = control.GetHashCode();
			return LoadedControls.Contains(code);
		}

	}
}
