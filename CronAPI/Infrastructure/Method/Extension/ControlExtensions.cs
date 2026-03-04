using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using WinformLib;
using static WinformLib.CustomizeFormsExtentions;
using static WinformLib.FlowLayoutPanelExtentions;

namespace CronAPI.Infrastructure.Method.Extension
{
    public static class ControlExtensions
    {
        public static void AddButtonsNew(this FlowLayoutPanel flowPanel, FlowLayOutListInput input, Action<int, string, Button> onBtnClick)
        {
            Action<int, string, Button> onBtnClick2 = onBtnClick;
            if (flowPanel == null)
            {
                throw new ArgumentNullException("flowPanel", "FlowLayoutPanel控件不能为空");
            }

            if (input == null)
            {
                throw new ArgumentNullException("input", "输入参数实体不能为空");
            }

            if (input.NameList == null || input.NameList.Count == 0)
            {
                throw new ArgumentException("按钮名称列表不能为空", "NameList");
            }

            flowPanel.Controls.Clear();
            flowPanel.FlowDirection = FlowDirection.TopDown;
            flowPanel.WrapContents = false;
            flowPanel.AutoScroll = false;
            flowPanel.Padding = new Padding(0);
            flowPanel.Margin = new Padding(0);
            flowPanel.AutoSize = false;
            int count = input.NameList.Count;
            for (int i = 0; i < input.NameList.Count; i++)
            {
                Padding margin = new Padding(0);
                if (i > 0)
                {
                    margin.Top = input.VerticalSpacing;
                }

                string text = input.NameList[i];
                int currentIndex = i;
                string currentBtnName = text;
                Button button = new Button
                {
                    Name = $"btn_{i}_{text}",
                    Text = text,
                    Margin = margin,
                    Width = flowPanel.ClientSize.Width,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.White,
                    Height = (flowPanel.ClientSize.Height - (count - 1) * input.VerticalSpacing) / count
                };
                button.Click += delegate
                {
                    onBtnClick2?.Invoke(currentIndex, currentBtnName, button);
                };
                flowPanel.Controls.Add(button);
            }
        }


        /// <summary>
        /// 获取子控件(使用对象：Panel类、GroupBox类等)
        /// </summary>
        public static List<T> GetChildrenControls<T>(this Control parent) where T : Control
        {
            return parent.Controls.OfType<T>().ToList();
        }


        /// <summary>
        /// 渲染DataGridView（可控制Cell/UI，加强版）
        /// </summary>
        public static void SetCommonWithCell<T>(this DataGridView dataGridView, DataDisplayEntityCell<T> input) where T : class, new()
        {
            // 先渲染数据
            dataGridView.SetCommon(input.DataList, input.HeadtextList, input.ButtonList.Select(x => x.ButtonName).ToList());
            dataGridView.ReadOnly = false;
            // 单独处理UI
            foreach (DataGridViewRow item in dataGridView.Rows)
            {
                // 行操作
                if (input.RowAction != null)
                {
                    input.RowAction(item.Tag as T, item);
                }

                //单元格操作
                foreach (DataGridViewCell cell in item.Cells)
                {
                    if (input.CellAction != null)
                    {
                        input.CellAction(item.Tag as T, cell.OwningColumn, cell);
                    }

                }
            }
            //列操作
            foreach (DataGridViewColumn item in dataGridView.Columns)
            {
                if (input.ColumnAction != null)
                {
                    input.ColumnAction(item);
                }
            }
            // 处理按钮宽度
            foreach (var item in input.ButtonList)
            {
                dataGridView.Columns[item.ButtonName].HeaderText = item.TitileName;
                dataGridView.Columns[item.ButtonName].Width = item.Width;
            }
        }

        /// <summary>
        /// UI设置入参Dto
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class DataDisplayEntityCell<T>
        {
            /// <summary>
            /// 数据列表
            /// </summary>
            public List<T> DataList { get; set; } = new List<T>();

            /// <summary>
            /// 字段、标题名称及宽度
            /// </summary>
            public List<(Expression<Func<T, object>> Feild, string TitileName, int Width)> HeadtextList { get; set; } = new List<(Expression<Func<T, object>> Feild, string TitileName, int width)>();

            /// <summary>
            /// 按钮名称、标题名称及宽度
            /// </summary>
            public List<(string ButtonName, string TitileName, int Width)> ButtonList { get; set; } = new List<(string ButtonName, string TitileName, int Width)>();

            /// <summary>
            /// 行样式样式委托（实体、实体对应的行）
            /// 示例：if (user.Name.Equals("李四"))row.DefaultCellStyle.ForeColor =  Color.Red;
            /// </summary>
            public Action<T, DataGridViewRow>? RowAction { get; set; } = null;

            /// <summary>
            /// 列按钮委托（列）
            /// 示例：if (col.Name.Equals("Name"))col.ReadOnly = false;
            /// </summary>
            public Action<DataGridViewColumn>? ColumnAction { get; set; } = null;

            /// <summary>
            /// 单元格样式委托（实体、当前列、通过实体和当前列筛选得到的单元格）
            /// 示例：if(user.Name.Equals("张三") && col.Name.Equals("Name"))cell.Style.BackColor = Color.Yellow;
            /// </summary>
            public Action<T, DataGridViewColumn, DataGridViewCell>? CellAction { get; set; } = null;

        }

        #region 自定义窗体2.0

        /// <summary>
        /// 自定义窗体UI及控件选项
        /// </summary>
        public class CustomizeFormInputNew
        {
            /// <summary>
            /// 内容选项
            /// </summary>
            public List<CustomizeValueInputNew> inputs { get; set; } = new List<CustomizeValueInputNew>();
            /// <summary>
            /// 自定义窗体的标题
            /// </summary>
            public string FormTitle { get; set; } = "自定义输入对话框";
            /// <summary>
            /// 标签和控件之间的距离(可选)
            /// </summary>
            public int LabelLocationX { get; set; } = -1;
            /// <summary>
            /// 自定义窗体的大小(可选)
            /// </summary>
            public (int Width, int Height) Size { get; set; } = (-1, -1);
        }

        /// <summary>
        /// 自定义窗体控件选项
        /// </summary>
        public class CustomizeValueInputNew
        {
            /// <summary>
            /// 控件之间的垂直上下距离
            /// </summary>
            public int VertiPadding { get; set; } = 50;
            /// <summary>
            /// 控件类型
            /// </summary>

            public FormControlTypeNew FormControlType { get; set; } = FormControlTypeNew.InputBox;
            /// <summary>
            /// 标签内容
            /// </summary>
            public string Label { get; set; } = string.Empty;
            /// <summary>
            /// 控件的值
            /// </summary>
            public List<string> Value { get; set; } = new List<string>();
            /// <summary>
            /// 默认值(英文逗号分隔)
            /// </summary>
            public string DefaultValue { get; set; } = string.Empty;
        }

        /// <summary>
        /// 自定义窗体控件选项
        /// </summary>
        public enum FormControlTypeNew
        {
            InputBox = 1,     // 输入框
            DropDown = 2,     // 下拉框
            RadioButton = 3,  // 单选框
            CheckBox = 4,     // 复选框
            NumberBox = 5     // 数字框
        }

        /// <summary>
        /// 自定义窗体方法
        /// </summary>
        public static Dictionary<string, string> SetCustomizeFormsNew(this Form form, CustomizeFormInputNew inputDto)
        {
            // 基础验证
            var labels = inputDto.inputs.Select(x => x.Label).ToList();
            var labels_Distinct = labels.Distinct().ToList();
            if (labels.Count != labels_Distinct.Count)
            {
                throw new Exception("Label不允许重名！");
            }

            var inputs = inputDto.inputs;
            // 创建自定义窗体
            Form inputForm = new Form
            {
                Text = inputDto.FormTitle,
                MaximizeBox = false,
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedSingle,
                Icon = form.Icon //继承原来的窗体的图标
            };

            // 常量声明，统一维护
            int defualtHeight = 30;
            int currentY = 15;
            int FormPadding = 30;//窗体内边距
            int defualtControlWidth = 250;//默认控件宽度（textbox,combobox）
            int horizontalSpacing = 10; // 单选框/复选框横向间距（解决文本重叠）
            int btnTopMargin = 10; // 按钮与上方控件的间距（解决高度不足）

            // 存储各控件组
            Dictionary<string, List<CheckBox>> checkBoxGroups = new Dictionary<string, List<CheckBox>>();
            Dictionary<string, List<RadioButton>> radioButtonGroups = new Dictionary<string, List<RadioButton>>();
            Dictionary<string, TextBox> textBoxes = new Dictionary<string, TextBox>();
            Dictionary<string, NumericUpDown> NumberBoxes = new Dictionary<string, NumericUpDown>();
            Dictionary<string, ComboBox> comboBoxes = new Dictionary<string, ComboBox>();

            // 算出最大标签宽度
            var maxLabel = inputs.MaxBy(x => x.Label.Length)?.Label;
            int maxLabelWidth = GetLabelWith(maxLabel);
            var LabelAndValuePadding = inputDto.LabelLocationX > 0 ? inputDto.LabelLocationX : maxLabelWidth + 10;

            //算出控件最大宽度
            int maxControlWidth = defualtControlWidth;

            //添加控件
            foreach (var input in inputs)
            {
                // 添加标签
                Label label = new Label
                {
                    Text = input.Label,
                    Location = new System.Drawing.Point(FormPadding, currentY + 2),
                    AutoSize = true
                };
                inputForm.Controls.Add(label);
               

                switch (input.FormControlType)
                {
                    case FormControlTypeNew.InputBox:
                        TextBox textBox = new TextBox
                        {
                            Location = new System.Drawing.Point(FormPadding + LabelAndValuePadding, currentY),
                            Width = defualtControlWidth,
                            Height = defualtHeight
                        };
                        inputForm.Controls.Add(textBox);
                        textBoxes[input.Label] = textBox;
                        if (!string.IsNullOrEmpty(input.DefaultValue))
                        {
                            textBox.Text = input.DefaultValue;
                        }
                        break;
                    case FormControlTypeNew.NumberBox:
                        NumericUpDown number = new NumericUpDown
                        {
                            Location = new System.Drawing.Point(FormPadding + LabelAndValuePadding, currentY),
                            Width = defualtControlWidth,
                            Height = defualtHeight
                        };
                        inputForm.Controls.Add(number);
                        NumberBoxes[input.Label] = number;
                        if (!string.IsNullOrEmpty(input.DefaultValue))
                        {
                            number.Value = decimal.TryParse(input.DefaultValue, out var outDecimal) ? outDecimal : 0;
                        }
                        break;

                    case FormControlTypeNew.DropDown:
                        ComboBox comboBox = new ComboBox
                        {
                            Location = new System.Drawing.Point(FormPadding + LabelAndValuePadding, currentY),
                            Width = defualtControlWidth,
                            Height = defualtHeight,
                            DropDownStyle = ComboBoxStyle.DropDownList
                        };
                        comboBox.Items.AddRange(input.Value.ToArray());
                        inputForm.Controls.Add(comboBox);
                        comboBoxes[input.Label] = comboBox;
                        if (!string.IsNullOrEmpty(input.DefaultValue))
                        {
                            comboBox.SetCommonItems(input.DefaultValue);
                        }
                        else
                        {
                            if (comboBox.Items != null && comboBox.Items.Count > 0)
                            {
                                comboBox.SelectedIndex = 0;
                            }
                        }
                        break;

                    case FormControlTypeNew.RadioButton:
                        #region 修复核心：为每组单选框创建独立Panel容器
                        int radioButtonX = 0;
                        // 创建Panel，承载当前组所有单选框，解决全局互斥问题
                        Panel radioPanel = new Panel
                        {
                            Location = new System.Drawing.Point(FormPadding + LabelAndValuePadding, currentY),
                            Height = defualtHeight,
                            Width = 0 // 宽度动态计算
                        };
                        radioButtonGroups[input.Label] = new List<RadioButton>();
                        foreach (var value in input.Value)
                        {
                            RadioButton radioButton = new RadioButton
                            {
                                Text = value,
                                Location = new System.Drawing.Point(radioButtonX, 0), // Panel内X轴从0开始
                                AutoSize = true,
                                Height = defualtHeight
                            };
                            radioButtonGroups[input.Label].Add(radioButton);
                            if (!string.IsNullOrEmpty(input.DefaultValue) && value == input.DefaultValue)
                            {
                                radioButton.Checked = true;
                            }
                            radioPanel.Controls.Add(radioButton);
                            // 累加Panel内控件位置，加间距
                            radioButtonX += radioButton.Width + horizontalSpacing;
                        }
                        // 设置Panel的实际宽度，刚好包裹所有单选框
                        radioPanel.Width = radioButtonX + horizontalSpacing;
                        inputForm.Controls.Add(radioPanel);
                        // 更新最大控件宽度，适配窗体
                        maxControlWidth = Math.Max(maxControlWidth, radioPanel.Width);
                        #endregion
                        break;

                    case FormControlTypeNew.CheckBox:
                        #region 优化：为每组复选框创建独立Panel容器，解决横向溢出
                        int checkButtonX = 0;
                        var defaultValues = !string.IsNullOrEmpty(input.DefaultValue) ? input.DefaultValue.Split(',').ToList() : new List<string>();
                        // 创建Panel，承载当前组所有复选框
                        Panel checkPanel = new Panel
                        {
                            Location = new System.Drawing.Point(FormPadding + LabelAndValuePadding, currentY),
                            Height = defualtHeight,
                            Width = 0
                        };
                        checkBoxGroups[input.Label] = new List<CheckBox>();
                        foreach (var value in input.Value)
                        {
                            CheckBox checkBox = new CheckBox
                            {
                                Text = value,
                                Location = new System.Drawing.Point(checkButtonX, 0), // Panel内X轴从0开始
                                AutoSize = true,
                                Height = defualtHeight
                            };
                            checkBoxGroups[input.Label].Add(checkBox);
                            if (defaultValues.Contains(value))
                            {
                                checkBox.Checked = true;
                            }
                            checkPanel.Controls.Add(checkBox);
                            // 累加Panel内控件位置，加间距
                            checkButtonX += checkBox.Width + horizontalSpacing;
                        }
                        // 设置Panel的实际宽度，刚好包裹所有复选框
                        checkPanel.Width = checkButtonX + horizontalSpacing;
                        inputForm.Controls.Add(checkPanel);
                        // 更新最大控件宽度，适配窗体
                        maxControlWidth = Math.Max(maxControlWidth, checkPanel.Width);
                        #endregion
                        break;
                }
                // 保底最小值，防止控件垂直重叠
                currentY += Math.Max(input.VertiPadding, defualtHeight + 5);
            }

            // 重新计算窗体宽度，确保所有控件都能完整显示
            var FormWidth = FormPadding * 3 + LabelAndValuePadding + maxControlWidth;
            inputForm.Width = FormWidth;

            // 所有控件同宽度
            foreach (Control item in inputForm.Controls)
            {
                item.Width = maxControlWidth;
                if (item is Label)
                {
                    item.SendToBack();
                }
            }

            // 创建一个任务以便返回选定的值
            var tcs = new Dictionary<string, string>();

            // 创建确认按钮
            Button btnOK = new Button
            {
                Text = "确定",
                Height = defualtHeight,
                Width = 80,
                Location = new System.Drawing.Point(FormPadding + LabelAndValuePadding, currentY + btnTopMargin)
            };
            btnOK.Click += (s, args) =>
            {
                tcs.Clear();
                // 获取输入框的值
                foreach (var textBox in textBoxes)
                {
                    tcs[textBox.Key] = textBox.Value.Text;
                }

                // 获取数字框的值
                foreach (var number in NumberBoxes)
                {
                    tcs[number.Key] = number.Value.Text;
                }

                // 获取下拉框的值
                foreach (var comboBox in comboBoxes)
                {
                    tcs[comboBox.Key] = comboBox.Value.SelectedItem?.ToString() ?? string.Empty;
                }

                // 获取复选框的值
                foreach (var group in checkBoxGroups)
                {
                    List<string> selectedCheckBoxValues = group.Value.Where(cb => cb.Checked).Select(cb => cb.Text).ToList();
                    tcs[group.Key] = string.Join(",", selectedCheckBoxValues);
                }

                // 获取单选框的值【完全正常：组内单选、组间独立】
                foreach (var group in radioButtonGroups)
                {
                    string selectedRadioButtonValue = group.Value.FirstOrDefault(rb => rb.Checked)?.Text ?? string.Empty;
                    tcs[group.Key] = selectedRadioButtonValue;
                }

                inputForm.Close();
            };

            // 创建取消按钮
            Button btnCancel = new Button
            {
                Text = "取消",
                Height = defualtHeight,
                Width = 80,
                Location = new System.Drawing.Point(btnOK.Right + 80, currentY + btnTopMargin)
            };

            btnCancel.Click += (s, args) =>
            {
                tcs.Clear();
                inputForm.Close();
            };

            // 将按钮添加到窗体
            inputForm.Controls.Add(btnOK);
            inputForm.Controls.Add(btnCancel);

            // 核心修复：设置客户区高度，按钮完整显示，无遮挡
            inputForm.ClientSize = new Size(inputForm.ClientSize.Width, btnOK.Bottom + FormPadding);

            // 用户自定义大小
            if (inputDto.Size.Width > 0 && inputDto.Size.Height > 0)
            {
                inputForm.Width = inputDto.Size.Width;
                inputForm.Height = inputDto.Size.Height;
            }

            //调整按钮位置
            var btnOk_X = (inputForm.ClientSize.Width - 80 * 3) / 2;
            var btnCancel_X = btnOk_X + 80 * 2;
            btnOK.Location = new Point(btnOk_X, currentY + btnTopMargin);
            btnCancel.Location = new Point(btnCancel_X, currentY + btnTopMargin);


            // 显示窗体并等待用户操作
            inputForm.ShowDialog();

            // 返回用户输入的结果
            return tcs;
        }

        /// <summary>
        /// 获取label字符串的宽度
        /// </summary>
        /// <param name="maxLabel"></param>
        /// <returns></returns>
        private static int GetLabelWith(string? maxLabel)
        {
            Label lbl = new Label();
            lbl.Text = maxLabel;
            //lbl.Font = new System.Drawing.Font("宋体",15) ;//如果有字体格式还要设置好，可以用默认的
            Graphics g = Graphics.FromHwnd(lbl.Handle);
            SizeF size = g.MeasureString(lbl.Text, lbl.Font);//获取大小
            g.Dispose();
            return Convert.ToInt32(size.Width);
        }
        #endregion

        #region 文件上传

        /// <summary>
        /// 允许面板接收文件拖放(多个)
        /// </summary>
        /// <param name="panel">需要开启拖放功能的面板</param>
        public static void ReceiveMutiFiles(this Panel panel, Action<List<string>> funs)
        {
            // 1. 初始化面板基础属性（程序启动时立即生效）
            panel.AllowDrop = true;
            panel.BorderStyle = BorderStyle.FixedSingle;
            panel.BackColor = Color.DarkGray; // 初始背景色，打开就显示Gray

            // 2. 拖入事件：鼠标进入面板且有文件时
            panel.DragEnter += (sender, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    e.Effect = DragDropEffects.All;
                    panel.BackColor = Color.LightGray; // 拖入时高亮（可选，区分初始状态）
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            };

            // 3. 拖离事件：鼠标拖入后未放下就离开面板，恢复初始色
            panel.DragLeave += (sender, e) =>
            {
                panel.BackColor = Color.DarkSlateGray;
            };

            // 4. 拖放完成事件：放下文件后
            panel.DragDrop += (sender, e) =>
            {
                // 放下文件后设置为DarkGray
                panel.BackColor = Color.DarkGray;

                string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
                List<string> result = new List<string>();
                if (filePaths != null && filePaths.Length > 0)
                {
                    foreach (string filePath in filePaths)
                    {
                        if (File.Exists(filePath))
                        {
                            result.Add(filePath);
                        }
                    }
                    funs(result);
                }
            };
        }

        /// <summary>
        /// 允许面板接收文件拖放(单个)
        /// </summary>
        /// <param name="panel">需要开启拖放功能的面板</param>
        public static void ReceiveFiles(this Panel panel, Action<string> funs)
        {
            panel.ReceiveMutiFiles((list) =>
            {
                funs(list.FirstOrDefault() ?? string.Empty);
            });
        }
        #endregion
    }
}
