using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace ClaudeCodeHelper
{
    /// <summary>
    /// MainWindow의 상호작용 로직. 경로 입력/실행/목록 관리를 처리한다.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>경로 히스토리 관리자.</summary>
        private readonly PathManager _pathManager;

        /// <summary>초기화 중 체크박스 이벤트로 인한 불필요한 저장을 막는 가드.</summary>
        private bool _initializing = true;

        /// <summary>
        /// 윈도우를 생성하고 저장된 경로 목록을 불러온다.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            _pathManager = new PathManager(AppConfig.GetDefaultConfigPath());
            _pathManager.Load();
            RefreshPathList();

            txtPath.Text = _pathManager.LastUsedPath;
            chkFullPermission.IsChecked = _pathManager.FullPermissionMode;

            _initializing = false;
        }

        #region UI Helpers
        /// <summary>
        /// 경로 목록 ListBox를 현재 히스토리로 다시 채운다.
        /// </summary>
        private void RefreshPathList()
        {
            lstPaths.ItemsSource = null;
            lstPaths.ItemsSource = _pathManager.PathHistory;
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// "찾아보기" — 폴더 선택 다이얼로그로 경로를 지정한다.
        /// </summary>
        /// <param name="sender">이벤트 발생 컨트롤</param>
        /// <param name="e">이벤트 인자</param>
        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog dialog = new()
            {
                Title = "폴더 선택"
            };

            if (string.IsNullOrWhiteSpace(txtPath.Text) == false && Directory.Exists(txtPath.Text) == true)
            {
                dialog.InitialDirectory = txtPath.Text;
            }

            if (dialog.ShowDialog() == true)
            {
                txtPath.Text = dialog.FolderName;
            }
        }

        /// <summary>
        /// "실행" — 경로를 검증하고 해당 폴더에서 claude를 실행한다.
        /// </summary>
        /// <param name="sender">이벤트 발생 컨트롤</param>
        /// <param name="e">이벤트 인자</param>
        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            string path = txtPath.Text;

            if (string.IsNullOrWhiteSpace(path) == true)
            {
                MessageBox.Show("경로를 입력하세요.", "ClaudeCodeHelper", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (Directory.Exists(path) == false)
            {
                MessageBox.Show("존재하지 않는 경로입니다.", "ClaudeCodeHelper", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _pathManager.AddPath(path);
            _pathManager.Save();
            RefreshPathList();

            try
            {
                ProcessLauncher.LaunchClaude(path, chkFullPermission.IsChecked == true);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"실행 중 오류가 발생했습니다:\n{ex.Message}", "ClaudeCodeHelper", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// "모든 권한으로 실행" 체크 변경 — 상태를 config에 저장한다.
        /// </summary>
        /// <param name="sender">이벤트 발생 컨트롤</param>
        /// <param name="e">이벤트 인자</param>
        private void ChkFullPermission_Changed(object sender, RoutedEventArgs e)
        {
            if (_initializing == true)
            {
                return;
            }

            _pathManager.FullPermissionMode = chkFullPermission.IsChecked == true;
            _pathManager.Save();
        }

        /// <summary>
        /// 목록 선택 변경 — 선택한 경로를 텍스트박스에 반영한다.
        /// </summary>
        /// <param name="sender">이벤트 발생 컨트롤</param>
        /// <param name="e">선택 변경 인자</param>
        private void LstPaths_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstPaths.SelectedItem == null)
            {
                return;
            }

            txtPath.Text = lstPaths.SelectedItem.ToString();
        }

        /// <summary>
        /// "삭제" — 선택한 경로를 목록에서 제거한다.
        /// </summary>
        /// <param name="sender">이벤트 발생 컨트롤</param>
        /// <param name="e">이벤트 인자</param>
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (lstPaths.SelectedItem == null)
            {
                MessageBox.Show("삭제할 경로를 선택하세요.", "ClaudeCodeHelper", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string? selected = lstPaths.SelectedItem.ToString();
            if (selected == null)
            {
                return;
            }

            MessageBoxResult result = MessageBox.Show("선택한 경로를 삭제하시겠습니까?", "ClaudeCodeHelper", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            _pathManager.RemovePath(selected);
            _pathManager.Save();
            RefreshPathList();
        }

        /// <summary>
        /// "전체 삭제" — 모든 경로를 제거한다.
        /// </summary>
        /// <param name="sender">이벤트 발생 컨트롤</param>
        /// <param name="e">이벤트 인자</param>
        private void BtnClearAll_Click(object sender, RoutedEventArgs e)
        {
            if (_pathManager.PathHistory.Count == 0)
            {
                return;
            }

            MessageBoxResult result = MessageBox.Show("저장된 모든 경로를 삭제하시겠습니까?", "ClaudeCodeHelper", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            _pathManager.ClearAll();
            _pathManager.Save();
            RefreshPathList();
        }

        /// <summary>
        /// "폴더 열기" — 선택/입력한 경로를 탐색기로 연다.
        /// </summary>
        /// <param name="sender">이벤트 발생 컨트롤</param>
        /// <param name="e">이벤트 인자</param>
        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            string path = txtPath.Text;

            if (string.IsNullOrWhiteSpace(path) == true)
            {
                MessageBox.Show("열 경로를 선택하세요.", "ClaudeCodeHelper", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (Directory.Exists(path) == false)
            {
                MessageBox.Show($"경로를 찾을 수 없습니다:\n{path}", "ClaudeCodeHelper", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                ProcessLauncher.OpenFolder(path);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"폴더를 열 수 없습니다:\n{ex.Message}", "ClaudeCodeHelper", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion
    }
}
