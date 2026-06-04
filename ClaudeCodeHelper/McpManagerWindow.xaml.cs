using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ClaudeCodeHelper
{
    /// <summary>
    /// MCP 관리 창. 선택한 여러 프로젝트에 프리셋/직접입력 MCP를 일괄 추가·제거한다.
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

        /// <summary>선택된 스코프 문자열을 반환한다.</summary>
        /// <returns>"local" 또는 "project"</returns>
        private string GetSelectedScope()
        {
            return rbProject.IsChecked == true ? "project" : "local";
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
            btnInstall.IsEnabled = enabled;
            btnRemove.IsEnabled = enabled;
        }
        #endregion

        #region Event Handlers
        /// <summary>프로젝트 선택 변경 — 설치 목록 갱신.</summary>
        private void LstProjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshInstalledList();
        }

        /// <summary>"프리셋 추가" — 입력 모달로 새 프리셋을 등록한다.</summary>
        private void BtnAddPreset_Click(object sender, RoutedEventArgs e)
        {
            McpPresetDialog dialog = new() { Owner = this };
            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                _presetStore.AddOrUpdate(dialog.Result);
                _presetStore.Save();
                RefreshPresetList();
            }
        }

        /// <summary>"편집" — 선택 프리셋을 모달로 수정한다.</summary>
        private void BtnEditPreset_Click(object sender, RoutedEventArgs e)
        {
            if (lstPresets.SelectedItem is not McpPreset selected)
            {
                MessageBox.Show("편집할 프리셋을 선택하세요.", "MCP 관리", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            McpPresetDialog dialog = new(selected) { Owner = this };
            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                _presetStore.AddOrUpdate(dialog.Result);
                _presetStore.Save();
                RefreshPresetList();
            }
        }

        /// <summary>"삭제" — 선택 프리셋을 제거한다.</summary>
        private void BtnDeletePreset_Click(object sender, RoutedEventArgs e)
        {
            if (lstPresets.SelectedItem is not McpPreset selected)
            {
                MessageBox.Show("삭제할 프리셋을 선택하세요.", "MCP 관리", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBoxResult result = MessageBox.Show($"프리셋 '{selected.Name}'을(를) 삭제하시겠습니까?", "MCP 관리", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            _presetStore.Remove(selected.Name);
            _presetStore.Save();
            RefreshPresetList();
        }

        /// <summary>"직접 입력" — 저장 없이 1회용 프리셋으로 즉시 설치한다.</summary>
        private async void BtnDirectInput_Click(object sender, RoutedEventArgs e)
        {
            McpPresetDialog dialog = new() { Owner = this };
            if (dialog.ShowDialog() == true && dialog.Result != null)
            {
                await InstallPresetsAsync(new List<McpPreset> { dialog.Result });
            }
        }

        /// <summary>"선택 프로젝트에 추가" — 선택 프리셋들을 선택 프로젝트들에 설치한다.</summary>
        private async void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            List<McpPreset> presets = new();
            foreach (object item in lstPresets.SelectedItems)
            {
                if (item is McpPreset preset)
                {
                    presets.Add(preset);
                }
            }

            if (presets.Count == 0)
            {
                MessageBox.Show("설치할 프리셋을 선택하세요.", "MCP 관리", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            await InstallPresetsAsync(presets);
        }

        /// <summary>"선택 항목 제거" — 선택 설치 MCP를 선택 프로젝트들에서 제거한다.</summary>
        private async void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            List<string> projects = GetSelectedProjects();
            if (projects.Count == 0)
            {
                MessageBox.Show("대상 프로젝트를 선택하세요.", "MCP 관리", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show("제거할 MCP를 '설치된 MCP' 목록에서 선택하세요.", "MCP 관리", MessageBoxButton.OK, MessageBoxImage.Information);
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

        #region Batch
        /// <summary>선택된 프로젝트 경로 목록을 반환한다.</summary>
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

        /// <summary>
        /// 선택된 프로젝트들에 주어진 프리셋들을 설치한다(백그라운드 실행).
        /// </summary>
        /// <param name="presets">설치할 프리셋 목록</param>
        private async Task InstallPresetsAsync(List<McpPreset> presets)
        {
            List<string> projects = GetSelectedProjects();
            if (projects.Count == 0)
            {
                MessageBox.Show("대상 프로젝트를 선택하세요.", "MCP 관리", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string scope = GetSelectedScope();
            SetActionsEnabled(false);
            AppendLog("=== 추가 시작 ===");

            await Task.Run(() =>
            {
                foreach (string project in projects)
                {
                    foreach (McpPreset preset in presets)
                    {
                        McpCommandResult result = McpRunner.Add(project, preset, scope);
                        Dispatcher.Invoke(() =>
                        {
                            AppendLog($"[{(result.Success ? "OK" : "ERR")}] {project} : add {preset.Name} ({scope})");
                            if (string.IsNullOrEmpty(result.Output) == false)
                            {
                                AppendLog("    " + result.Output);
                            }
                            if (result.Success == true)
                            {
                                _tracker.RecordAdd(project, preset.Name, scope);
                            }
                        });
                    }
                }
            });

            _tracker.Save();
            RefreshInstalledList();
            AppendLog("=== 추가 완료 ===");
            SetActionsEnabled(true);
        }
        #endregion
    }
}
