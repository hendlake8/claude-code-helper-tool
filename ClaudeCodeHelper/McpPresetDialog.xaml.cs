using System;
using System.Collections.Generic;
using System.Windows;

namespace ClaudeCodeHelper
{
    /// <summary>
    /// MCP 프리셋 입력 모달. 추가/편집/직접입력에 공용으로 사용한다.
    /// </summary>
    public partial class McpPresetDialog : Window
    {
        /// <summary>확인 시 채워지는 결과 프리셋.</summary>
        public McpPreset? Result { get; private set; }

        /// <summary>
        /// 신규 입력 또는 기존 프리셋 편집용으로 생성한다.
        /// </summary>
        /// <param name="existing">편집 대상 프리셋. null이면 신규 입력</param>
        public McpPresetDialog(McpPreset? existing = null)
        {
            InitializeComponent();

            if (existing != null)
            {
                txtName.Text = existing.Name;
                txtCommand.Text = existing.Command;
                txtArgs.Text = string.Join(Environment.NewLine, existing.Args);
            }
        }

        /// <summary>
        /// "확인" — 입력값을 검증하고 Result를 채운다.
        /// </summary>
        /// <param name="sender">이벤트 발생 컨트롤</param>
        /// <param name="e">이벤트 인자</param>
        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            string name = txtName.Text.Trim();
            string command = txtCommand.Text.Trim();

            if (string.IsNullOrWhiteSpace(name) == true)
            {
                MessageBox.Show("이름을 입력하세요.", "MCP 프리셋", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(command) == true)
            {
                MessageBox.Show("명령을 입력하세요.", "MCP 프리셋", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            List<string> args = new();
            string[] lines = txtArgs.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) == false)
                {
                    args.Add(trimmed);
                }
            }

            Result = new McpPreset
            {
                Name = name,
                Command = command,
                Args = args
            };

            DialogResult = true;
        }
    }
}
