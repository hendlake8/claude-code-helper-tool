using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ClaudeCodeHelper
{
    /// <summary>
    /// 로컬MCP 관리 창. 선택한 여러 프로젝트에 통문장 명령을 실행해 MCP를 추가하고,
    /// local 스코프 설치분을 추적·제거한다.
    /// </summary>
    public partial class McpManagerWindow : Window
    {
        /// <summary>프로젝트 경로 제공(런처의 PathHistory 재사용).</summary>
        private readonly PathManager _pathManager;

        /// <summary>프리셋 저장소.</summary>
        private readonly McpPresetStore _presetStore;

        /// <summary>프로젝트별 설치 기록.</summary>
        private readonly McpInstallTracker _tracker;

        /// <summary>
        /// 창을 생성하고 프리셋/설치기록을 불러온다.
        /// </summary>
        /// <param name="pathManager">프로젝트 경로 제공자</param>
        public McpManagerWindow(PathManager pathManager)
        {
            InitializeComponent();

            _pathManager = pathManager;
            _presetStore = new McpPresetStore(McpPresetStore.GetDefaultPath());
            _presetStore.Load();
            _tracker = new McpInstallTracker(McpInstallTracker.GetDefaultPath());
            _tracker.Load();

            RefreshProjectList();
            RefreshPresetList();
        }

        #region UI Helpers
        /// <summary>프로젝트 목록을 PathHistory로 채운다.</summary>
        private void RefreshProjectList()
        {
            lstProjects.ItemsSource = null;
            lstProjects.ItemsSource = _pathManager.PathHistory;
        }

        /// <summary>프리셋 목록을 다시 채운다.</summary>
        private void RefreshPresetList()
        {
            lstPresets.ItemsSource = null;
            lstPresets.ItemsSource = _presetStore.Presets;
        }

        /// <summary>현재 선택(주) 프로젝트의 설치 목록을 표시한다.</summary>
        private void RefreshInstalledList()
        {
            string? project = lstProjects.SelectedItem as string;
            lstInstalled.ItemsSource = null;

            if (project == null)
            {
                return;
            }

            lstInstalled.ItemsSource = _tracker.GetInstalled(project);
        }

        /// <summary>로그 한 줄을 추가하고 끝으로 스크롤한다.</summary>
        /// <param name="line">로그 내용</param>
        private void AppendLog(string line)
        {
            txtLog.AppendText(line + Environment.NewLine);
            txtLog.ScrollToEnd();
        }

        /// <summary>배치 실행 중 버튼을 비활성화/복원한다.</summary>
        /// <param name="enabled">활성 여부</param>
        private void SetActionsEnabled(bool enabled)
        {
            btnRun.IsEnabled = enabled;
            btnRemove.IsEnabled = enabled;
        }

        /// <summary>선택된 프로젝트 경로 목록을 반환한다.</summary>
        /// <returns>선택된 프로젝트 경로 목록</returns>
        private List<string> GetSelectedProjects()
        {
            List<string> projects = new();
            foreach (object item in lstProjects.SelectedItems)
            {
                if (item is string path)
                {
                    projects.Add(path);
                }
            }
            return projects;
        }
        #endregion

        #region Event Handlers
        /// <summary>프로젝트 선택 변경 — 설치 목록 갱신.</summary>
        private void LstProjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshInstalledList();
        }

        /// <summary>프리셋 선택 변경 — 명령 입력 필드와 이름 필드를 채운다.</summary>
        private void LstPresets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstPresets.SelectedItem is not McpPreset selected)
            {
                return;
            }

            txtPresetName.Text = selected.Name;
            txtCommand.Text = selected.CommandLine;
        }

        /// <summary>"프리셋으로 저장" — 현재 이름+명령을 프리셋으로 저장한다.</summary>
        private void BtnSavePreset_Click(object sender, RoutedEventArgs e)
        {
            string name = txtPresetName.Text.Trim();

            if (string.IsNullOrWhiteSpace(name) == true)
            {
                MessageBox.Show("프리셋 이름을 입력하세요.", "로컬MCP 관리", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtCommand.Text) == true)
            {
                MessageBox.Show("저장할 명령을 입력하세요.", "로컬MCP 관리", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 명령문은 사용자 입력 원본 그대로 저장(개행 차단은 입력 단계에서 처리됨).
            _presetStore.AddOrUpdate(new McpPreset { Name = name, CommandLine = txtCommand.Text });
            _presetStore.Save();
            RefreshPresetList();
            AppendLog($"[저장] 프리셋 '{name}'");
        }

        /// <summary>"프리셋 삭제" — 선택 프리셋을 제거한다.</summary>
        private void BtnDeletePreset_Click(object sender, RoutedEventArgs e)
        {
            if (lstPresets.SelectedItem is not McpPreset selected)
            {
                MessageBox.Show("삭제할 프리셋을 선택하세요.", "로컬MCP 관리", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBoxResult result = MessageBox.Show($"프리셋 '{selected.Name}'을(를) 삭제하시겠습니까?", "로컬MCP 관리", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            _presetStore.Remove(selected.Name);
            _presetStore.Save();
            RefreshPresetList();
        }

        /// <summary>"선택 프로젝트에서 실행" — 명령 입력 내용을 선택 프로젝트들에서 실행한다.</summary>
        private async void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            List<string> projects = GetSelectedProjects();
            if (projects.Count == 0)
            {
                MessageBox.Show("대상 프로젝트를 선택하세요.", "로컬MCP 관리", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 사용자 입력 원본을 그대로 사용(가공 금지).
            string commandLine = txtCommand.Text;
            if (string.IsNullOrWhiteSpace(commandLine) == true)
            {
                MessageBox.Show("실행할 명령을 입력하세요.", "로컬MCP 관리", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SetActionsEnabled(false);
            AppendLog("=== 실행 시작 ===");

            await Task.Run(() =>
            {
                foreach (string project in projects)
                {
                    McpCommandResult result = McpRunner.RunCommandLine(project, commandLine);
                    Dispatcher.Invoke(() =>
                    {
                        AppendLog($"[{(result.Success ? "OK" : "ERR")}] {project} : {commandLine}");
                        if (string.IsNullOrEmpty(result.Output) == false)
                        {
                            AppendLog("    " + result.Output);
                        }

                        if (result.Success == true)
                        {
                            RecordIfLocal(project, commandLine);
                        }
                    });
                }
            });

            _tracker.Save();
            RefreshInstalledList();
            AppendLog("=== 실행 완료 ===");
            SetActionsEnabled(true);
        }

        /// <summary>"선택 항목 제거" — 선택 설치 MCP를 선택 프로젝트들에서 제거한다.</summary>
        private async void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            List<string> projects = GetSelectedProjects();
            if (projects.Count == 0)
            {
                MessageBox.Show("대상 프로젝트를 선택하세요.", "로컬MCP 관리", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            List<InstalledMcp> targets = new();
            foreach (object item in lstInstalled.SelectedItems)
            {
                if (item is InstalledMcp mcp)
                {
                    targets.Add(mcp);
                }
            }

            if (targets.Count == 0)
            {
                MessageBox.Show("제거할 MCP를 '설치된 MCP' 목록에서 선택하세요.", "로컬MCP 관리", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SetActionsEnabled(false);
            AppendLog("=== 제거 시작 ===");

            await Task.Run(() =>
            {
                foreach (string project in projects)
                {
                    foreach (InstalledMcp mcp in targets)
                    {
                        McpCommandResult result = McpRunner.Remove(project, mcp.Name, mcp.Scope);
                        Dispatcher.Invoke(() =>
                        {
                            AppendLog($"[{(result.Success ? "OK" : "ERR")}] {project} : remove {mcp.Name} ({mcp.Scope})");
                            if (string.IsNullOrEmpty(result.Output) == false)
                            {
                                AppendLog("    " + result.Output);
                            }
                            if (result.Success == true)
                            {
                                _tracker.RecordRemove(project, mcp.Name, mcp.Scope);
                            }
                        });
                    }
                }
            });

            _tracker.Save();
            RefreshInstalledList();
            AppendLog("=== 제거 완료 ===");
            SetActionsEnabled(true);
        }

        /// <summary>"새로고침" — 설치 목록을 다시 표시한다.</summary>
        private void BtnRefreshInstalled_Click(object sender, RoutedEventArgs e)
        {
            RefreshInstalledList();
        }
        #endregion

        #region Tracking
        /// <summary>
        /// 실행 성공한 명령문에서 name/scope를 파싱해 local 스코프만 설치 기록에 반영한다.
        /// 파싱 실패 또는 local이 아닌 스코프는 추적하지 않고 로그만 남긴다.
        /// </summary>
        /// <param name="project">대상 프로젝트 경로</param>
        /// <param name="commandLine">실행한 명령문</param>
        private void RecordIfLocal(string project, string commandLine)
        {
            if (McpRunner.TryParseAddTarget(commandLine, out string name, out string scope) == false)
            {
                AppendLog("    [추적불가] name 파싱 실패 — 제거 목록에 표시되지 않음");
                return;
            }

            if (scope != "local")
            {
                AppendLog($"    [추적안함] scope={scope} — 로컬 관리 대상 아님");
                return;
            }

            _tracker.RecordAdd(project, name, "local");
        }
        #endregion
    }
}
