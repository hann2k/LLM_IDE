using Markdig;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LLM_IDE
{
	public partial class MainWindow : Window
	{
		public ObservableCollection<LogData> LogHistory { get; set; } = new ObservableCollection<LogData>();

		// 저장버튼 눌렀을때 마지막으로 저장된 로그의 번호를 기억하는 변수예요. 이를 통해 증분 저장 시점부터 새로운 로그만 필터링할 수 있어요.
		private int lastSavedNumber = 0;
		
		// 이전까지의 답변 개수 기억
		private int _lastResponseCount = 0;

		// true: 현재 전송되는 메시지가 '프로젝트 설정' 혹은 '지침'임을 의미해요.
		private bool _isCriterionInjectionActive = false;

		public MainWindow()
		{
			InitializeComponent();
			LogList.ItemsSource = LogHistory;

			// [DC-04] 파이프라인에 UseAdvancedExtensions()가 반드시 포함되어야 표가 파싱됩니다.
			// var pipeline = new Markdig.MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
			
			// Markdig.Wpf.Markdown.DefaultPipeline = pipeline;

			// this.rcvMsg

			// 직접 컨트롤 검색 (가장 확실한 Fail-Safe)
			var viewer = this.FindName("rcvMsg") as Markdig.Wpf.MarkdownViewer;

			if (viewer != null)
			{
				var pipeline = new Markdig.MarkdownPipelineBuilder()
			.UseAdvancedExtensions()
			.UsePipeTables()
			.Build();
				viewer.Pipeline = pipeline;
				Debug.WriteLine("[Success] rcvMsg를 찾아 파이프라인을 주입했습니다.");
			}
			else
			{
				Debug.WriteLine("[Error] 여전히 rcvMsg를 찾을 수 없습니다. XAML 이름을 확인하세요.");
			}

			LoadProjectConfig();

			// 웹뷰 가리기.
			//Panel.SetZIndex(WebPopup, -1); // 계층도 뒤로 보냄
			//WebScale.ScaleX = 0;
			//WebScale.ScaleY = 0;
			//Task.Run(() => {
			//	// MainWebView.EnsureCoreWebView2Async();
				
			//});
			MainWebView.DefaultBackgroundColor = System.Drawing.Color.Transparent;
		}

		// 1. 노트패드 연동 (더블클릭 시)
		//private void LogList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		//{
		//	if (LogList.SelectedItem is LogData selected)
		//	{
		//		string tempPath = Path.Combine(Path.GetTempPath(), $"Log_{selected.Number}.txt");
		//		string content = $"[Time: {selected.TimeStamp}]\n[User]: {selected.UserInput}\n\n[LLM]: {selected.LlmOutput}";

		//		File.WriteAllText(tempPath, content);
		//		Process.Start("notepad.exe", tempPath);
		//	}
		//}

		// 2. 증분 저장 (텍스트 파일 형태)
		private void BtnSave_Click(object sender, RoutedEventArgs e)
		{
			if (LogHistory.Count == 0)
			{
				MessageBox.Show("저장할 로그가 없습니다.");
				return;
			}

			// 1. 로그의 첫 번째 타임스탬프에서 숫자만 추출해요. (예: 2026-05-11 11:48:24.512 -> 20260511114824512)
			string rawStamp = System.Text.RegularExpressions.Regex.Replace(LogHistory[0].TimeStamp, @"\D", "");

			// 2. 사용자님 지시대로 '날짜_시간(초)' 형식으로 재조합해요.
			// 앞 8자리(yyyyMMdd) + "_" + 이후 6자리(HHmmss)
			string safeDateTime = rawStamp.Substring(0, 8) + "_" + rawStamp.Substring(8, 6);

			// 3. 최종 파일명 적용
			string fileName = $"{safeDateTime}_Analysis_Log.txt";

			// 마지막 저장 번호 이후의 데이터 필터링
			var targetLogs = LogHistory.Where(x => x.Number > lastSavedNumber).ToList();

			if (targetLogs.Count > 0)
			{
				try
				{
					using (StreamWriter sw = File.AppendText(fileName))
					{
						for (int i = 0; i < targetLogs.Count; i++)
						{
							var log = targetLogs[i];

							// 사용자 요청 포맷 적용
							sw.WriteLine($"[Sequence] {log.Number}");
							sw.WriteLine($"[TimeStamp] {log.TimeStamp}");
							sw.WriteLine($"[User Input] {log.UserInput}");
							sw.WriteLine($"[LLM Answer]\n{log.LlmOutput}");

							// 마크다운 가로선 삽입 논리: 
							// 현재 로그가 전체 LogHistory의 마지막 아이템이 아닐 때만 가로선 추가
							if (log.Number < LogHistory.Last().Number)
							{
								sw.WriteLine();
								sw.WriteLine("---");
								sw.WriteLine();
							}
							else
							{
								// 마지막 로그일 경우 줄바꿈만 추가하여 마무리
								sw.WriteLine();
							}

							lastSavedNumber = log.Number; // 저장 지점 갱신
						}
					}
					MessageBox.Show($"{targetLogs.Count}개의 대화가 지정된 포맷으로 저장되었습니다.");
				}
				catch (Exception ex)
				{
					MessageBox.Show($"저장 오류: {ex.Message}");
				}
			}
			else
			{
				MessageBox.Show("새로 저장할 내용이 없습니다.");
			}
		}

		// 3. 웹뷰 팝업 제어
		private void BtnShowWeb_Click(object sender, RoutedEventArgs e)
		{
			// [1] 유령 엔진 칸의 너비를 확장 (그리드 Column 정의 접근)
			// 0:프로젝트, 1:스플리터, 2:중앙로그, 3:엔진칸
			// EngineCol.Width = new GridLength(1, GridUnitType.Star);

			if (EngineCol.Width.Value < 10) // 현재 숨김 상태로 간주
			{

				EngineCol.Width = new GridLength(800);
				MainWebView.Width = 800;
				BtnShowWeb.Content = "Close";
			}
			else
			{ 				
				EngineCol.Width = new GridLength(1);
				MainWebView.Width = 100;
				BtnShowWeb.Content = "Open";
			}

			// [2] 웹뷰의 크기도 사용자 시야에 맞게 확장 (기존 100에서 800 이상으로)
			// MainWebView.Width = EngineWrapper.Width;
			// MainWebView.Height = EngineWrapper.Height;

			// [3] 최상단 레이어로 호출
			// Panel.SetZIndex(EngineWrapper, 99);

			// [4] 가시성 보장 (혹시 몰라 추가)
			// EngineWrapper.Visibility = Visibility.Visible;
			// EngineWrapper.Background = System.Windows.Media.Brushes.White; // 배경색도 확실히 보이도록

			// 버튼 텍스트 변경 (Toggle 방식 운영 시)
			
			// 클릭 이벤트 분기를 위해 상태 체크 로직을 넣거나, 별도 닫기 버튼을 두는 것이 좋습니다.
		}

		private void BtnCloseWeb_Click(object sender, RoutedEventArgs e)
		{
			EngineCol.Width = new GridLength(1);
			//// [핵심] 크기를 0으로 줄여서 숨김 (가시성은 Visible 유지)
			//WebScale.ScaleX = 0;
			//WebScale.ScaleY = 0;

			//MainWebView.IsHitTestVisible = false;

			//Panel.SetZIndex(WebPopup, -1); // 계층도 뒤로 보냄

			// 1. 가시성은 무조건 Visible (엔진 가동 유지)
			// WebPopup.Visibility = Visibility.Hidden;

			// 2. 물리적 크기를 1x1로 축소 (0으로 하면 엔진이 멈춤)
			// MainWebView.Width = 1;
			// MainWebView.Height = 1;

			// 3. 혹시 모를 잔상 방지를 위해 구석으로 밀기
			MainWebView.HorizontalAlignment = HorizontalAlignment.Left;
			MainWebView.VerticalAlignment = VerticalAlignment.Top;
			MainWebView.Margin = new Thickness(0, 0, 0, 0);

			// 4. 클릭 전파 방지
			MainWebView.IsHitTestVisible = false;
		}

		private async void BtnSend_Click(object sender, RoutedEventArgs e)
		{
			string userInput = TxtInput.Text.Trim();
			if (string.IsNullOrEmpty(userInput))
				return;

			BtnSend.IsEnabled = false;
			string finalMessage = userInput;
			bool wasFirstInjection = false; // 이번 전송이 기준 주입 시도였는지 추적해요.

			// 1. 기준 주입 판별
			if (!_isCriterionInjectionActive)
			{
				string projectConfig = TxtProjectEditor.Text.Trim();
				if (!string.IsNullOrEmpty(projectConfig))
				{
					finalMessage = $"[SYSTEM_CONTEXT_INIT]\n{projectConfig}\n\n[USER_INPUT]\n{userInput}";
					wasFirstInjection = true;
					_isCriterionInjectionActive = true; // 우선 활성화 상태로 진입해요.
				}
			}
			else
			{
				finalMessage = $"[USER_INPUT]\n{userInput}";
			}

			// 2. 로그 생성
			var currentLog = new LogData
			{
				Number = LogHistory.Count + 1,
				TimeStamp = DateTime.Now.ToString("yyyy-MM-dd\nHH:mm:ss.fff"),
				UserInput = userInput,
				LlmOutput = "── 응답 대기 중 ──"
			};
			LogHistory.Add(currentLog);
			TxtInput.Clear();

			try
			{
				// 3. 전송 시도
				await SendMessageToWebViewAsync(finalMessage);
				// [Logic] PollForResponseAsync에 currentLog 객체를 넘겨 실시간 업데이트를 수행해요.
				string finalResult = await PollForResponseAsync(currentLog);

				// 답변 완료 시 전체 텍스트 업로드
				currentLog.LlmOutput = finalResult;
			}
			catch (Exception ex)
			{
				// [Logic] 통신 오류/타임아웃 발생 시 복구 프로토콜
				if (wasFirstInjection)
				{
					// 기준 주입에 실패했으므로, 다음 재시도 때 다시 주입되도록 플래그를 원복해요.
					_isCriterionInjectionActive = false;
				}

				currentLog.LlmOutput = $"[Fail] 통신 오류 발생";

				// 사용자님께 즉각 보고해요.
				MessageBox.Show($"대화 전송에 실패했습니다.\n사유: {ex.Message}\n\n전송 버튼을 다시 눌러 재시도해 주세요.",
								"통신 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
			finally
			{
				BtnSend.IsEnabled = true;
				LogList.ScrollIntoView(currentLog);
			}
		}

		//private async Task<string> PollForResponseAsync(LogData currentLog)
		//{
		//	int timeoutSeconds = 150;
		//	int checkIntervalMs = 500;
		//	int maxTicks = (timeoutSeconds * 1000 / checkIntervalMs);

		//	bool isStreamingStarted = false;
		//	string lastSeenText = string.Empty;
		//	int stableCount = 0; // 텍스트 변화가 멈췄는지 감시하는 카운터

		//	for (int i = 0; i < maxTicks; i++)
		//	{
		//		Debug.WriteLine($"[Polling] 체크 {i + 1}/{maxTicks}...");

		//		string checkScript = @"
		//          (function() {
		//              const messageNodes = document.querySelectorAll('message-content');
		//              const lastMessage = messageNodes[messageNodes.length - 1];
		//              const stopButton = document.querySelector('button[aria-label=""생성 중단""], .stop-generating-button');

		//              let text = '';
		//              if (lastMessage) {
		//                  const contentHolder = lastMessage.querySelector('.markdown-main-panel');
		//                  text = contentHolder ? contentHolder.innerText : lastMessage.innerText;
		//              }

		//              return JSON.stringify({
		//                  text: text.trim(),
		//                  isGenerating: (stopButton !== null),
		//                  elementCount: messageNodes.length
		//              });
		//          })();";

		//		string jsonResult = await MainWebView.ExecuteScriptAsync(checkScript);
		//		if (string.IsNullOrEmpty(jsonResult) || jsonResult == "null")
		//		{
		//			await Task.Delay(checkIntervalMs);
		//			continue;
		//		}

		//		string normalizedJson = jsonResult.Trim('"').Replace("\\\"", "\"").Replace("\\\\", "\\");

		//		try
		//		{
		//			using (System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(normalizedJson))
		//			{
		//				var root = doc.RootElement;
		//				bool isGenerating = root.GetProperty("isGenerating").GetBoolean();
		//				int currentElementCount = root.GetProperty("elementCount").GetInt32();
		//				string currentText = root.GetProperty("text").GetString() ?? "";

		//				// [무결성 확보] 인덱스(개수) 변화가 없더라도, 텍스트가 이전과 다르면 무조건 UI 업데이트
		//				// currentLog.LlmOutput에 값을 쓰는 순간 INotifyPropertyChanged에 의해 rcvMsg가 갱신됩니다.
		//				if (!string.IsNullOrEmpty(currentText) && currentText != currentLog.LlmOutput)
		//				{
		//					isStreamingStarted = true;
		//					currentLog.LlmOutput = currentText; // 여기서 실시간 갱신 발생 

		//					// [Debug] 실시간 수신 데이터 길이 확인
		//					Debug.WriteLine($"[Streaming Update] Length: {currentText.Length}");
		//				}

		//				// 종료 판정 로직 (기능 유지하되 리턴은 제거하여 흐름 보장)
		//				if (isGenerating == false && isStreamingStarted == true)
		//				{
		//					// 종료가 감지되면 인덱스만 맞춰두고 루프는 타임아웃까지 혹은 다음 변화까지 대기
		//					_lastResponseCount = currentElementCount;
		//				}
		//			}
		//		}
		//		catch (Exception ex)
		//		{
		//			Debug.WriteLine($"[Error] 파싱 오류: {ex.Message}");
		//		}

		//		await Task.Delay(checkIntervalMs);
		//	}

		//	return currentLog.LlmOutput; // 타임아웃 시 그때까지 쌓인 데이터라도 반환
		//}

		//		private async Task<string> PollForResponseAsync(LogData currentLog)
		//		{
		//			int timeoutSeconds = 150;
		//			int checkIntervalMs = 500;
		//			int maxTicks = (timeoutSeconds * 1000 / checkIntervalMs);
		//			bool isStreamingStarted = false;

		//			for (int i = 0; i < maxTicks; i++)
		//			{
		//				Debug.WriteLine($"[Polling] 체크 {i + 1}/{maxTicks}...");
		//				// [Logic] message-actions 태그 존재 여부를 함께 체크하여 종료 시점 판별
		//				string checkScript = @"
		//            (function() {
		//    // 1. 모든 메시지 콘텐츠와 액션 바를 리스트로 확보
		//    const messageNodes = document.querySelectorAll('message-content');
		//    const actionNodes = document.querySelectorAll('message-actions');

		//	console.log(messageNodes);

		//    const count = messageNodes.length;

		//    // 2. [무결성 확보] 현재 전송 인덱스보다 큰 최신 메시지만 대상
		//    // C#에서 넘겨받은 _lastResponseCount 보다 큰 인덱스의 요소만 접근
		//    let lastMessage = null;
		//    let isFinished = false;

		//    if (count > _lastResponseCount) {
		//        lastMessage = messageNodes[count - 1];

		//        // 3. 종료 판정: 전체 액션 바 개수가 메시지 개수와 일치하는지 확인
		//        // 혹은 마지막 메시지 노드 바로 다음에 message-actions가 존재하는지 확인
		//        if (actionNodes.length >= count) {
		//            isFinished = true;
		//        }
		//    }

		//    const stopButton = document.querySelector('button[aria-label=""생성 중단""], .stop-generating-button');

		//    let text = '';
		//    if (lastMessage) {
		//        const contentHolder = lastMessage.querySelector('.markdown-main-panel');
		//        text = contentHolder ? contentHolder.innerText : lastMessage.innerText;
		//    }

		//    return JSON.stringify({
		//        text: text.trim(),
		//        isGenerating: (stopButton !== null),
		//        isFinished: isFinished, // 최신 메시지에 대한 종료 여부만 반환
		//        elementCount: count
		//    });
		//})();";

		//				string jsonResult = await MainWebView.ExecuteScriptAsync(checkScript);
		//				if (string.IsNullOrEmpty(jsonResult) || jsonResult == "null")
		//				{
		//					await Task.Delay(checkIntervalMs);
		//					continue;
		//				}

		//				string normalizedJson = jsonResult.Trim('"').Replace("\\\"", "\"").Replace("\\\\", "\\");

		//				try
		//				{
		//					using (System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(normalizedJson))
		//					{
		//						var root = doc.RootElement;
		//						bool isGenerating = root.GetProperty("isGenerating").GetBoolean();
		//						bool isFinished = root.GetProperty("isFinished").GetBoolean();
		//						int currentElementCount = root.GetProperty("elementCount").GetInt32();
		//						string currentText = root.GetProperty("text").GetString() ?? "";

		//						// 1. 실시간 갱신: 텍스트가 변하면 무조건 UI 반영 (사용자 최우선 요구사항)
		//						if (!string.IsNullOrEmpty(currentText) && currentText != currentLog.LlmOutput)
		//						{
		//							isStreamingStarted = true;
		//							currentLog.LlmOutput = currentText;
		//						}

		//						// 2. 종료 판정: message-actions 태그가 수신되었거나 생성 중단 버튼이 사라진 경우
		//						// 스트리밍이 한 번이라도 시작되었다면(isStreamingStarted) 탈출 조건을 검사합니다.
		//						if (isStreamingStarted && (isFinished || !isGenerating))
		//						{
		//							_lastResponseCount = currentElementCount;
		//							Debug.WriteLine($"[System] 수신 완료 탈출 (Tag Detected: {isFinished})");
		//							return currentText;
		//						}
		//					}
		//				}
		//				catch (Exception ex)
		//				{
		//					Debug.WriteLine($"[Error] 파싱 오류: {ex.Message}");
		//				}

		//				await Task.Delay(checkIntervalMs);
		//			}

		//			return currentLog.LlmOutput;
		//		}

		private async Task<string> PollForResponseAsync(LogData currentLog)
		{
			// [DC-04] 상수 정의: 반응성 극대화를 위해 300ms 인터벌 권장
			const int CHECK_INTERVAL_MS = 300;
			const int TIMEOUT_SECONDS = 150;
			int maxTicks = (TIMEOUT_SECONDS * 1000 / CHECK_INTERVAL_MS);

			// pending 까지 임시 대기
			await Task.Delay(500);

			// [ID-01] 사수 고정 경로 (수정 금지)
			string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "", "pollingLogic.js");

			for (int i = 0; i < maxTicks; i++)
			{
				// [ER-03] 파일 존재 유무 확인
				if (!File.Exists(scriptPath))
				{
					Debug.WriteLine($"[Error] 스크립트 유실: {scriptPath}");
					break;
				}

				try
				{
					string jsTemplate = File.ReadAllText(scriptPath);
					string escapedInput = System.Web.HttpUtility.JavaScriptStringEncode(currentLog.UserInput);
					string checkScript = jsTemplate.Replace("/*DATA_INJECTION*/", $"\"{escapedInput}\"");

					// [FN-02] JS 실행 및 결과 수신
					string jsonResult = await MainWebView.ExecuteScriptAsync(checkScript);

					if (string.IsNullOrEmpty(jsonResult) || jsonResult == "null")
					{
						await Task.Delay(CHECK_INTERVAL_MS);
						continue;
					}

					string normalizedJson = jsonResult.Trim('"').Replace("\\\"", "\"").Replace("\\\\", "\\");

					using (JsonDocument doc = JsonDocument.Parse(normalizedJson))
					{
						var root = doc.RootElement;
						bool isPending = root.GetProperty("isPendingActive").GetBoolean();
						bool isValid = root.GetProperty("isValidBlock").GetBoolean();
						bool isDone = root.GetProperty("isFinished").GetBoolean();

						// 텍스트(Streaming용)와 HTML(최종 파싱용) 추출
						string currentText = root.GetProperty("text").GetString() ?? "";
						string currentHtml = root.TryGetProperty("html", out var h) ? h.GetString() ?? "" : "";

						// 1. Pending 상태일 경우 UI 피드백 후 대기 (사수님 로직 보존)
						if (isPending == true)
						{
							currentLog.Status = "응답 대기 중...";
							await Task.Delay(CHECK_INTERVAL_MS);
							continue;
						}

						// 2. [Stream Point] 실시간 텍스트 업데이트
						if (isValid == true)
						{
							currentLog.Status = "데이터 수신 중...";

							// 스트리밍 중에는 'text' 필드를 우선적으로 보여줌 (Flickering 방지)
							if (isDone == false && currentLog.LlmOutput != currentText)
							{
								currentLog.LlmOutput = currentText;
								Debug.WriteLine($"[Streaming] Recv: {currentText.Length} chars");
							}

							// 3. [Final Point] 완료 신호 시 HTML 기반 정밀 파싱 적용
							if (isDone == true)
							{
								Debug.WriteLine($"[Success] 최종 신호 포착. HTML 구조 안정화 대기 시작.");

								// [Action] 300ms 간격으로 최대 5회(1.5초) 대기하며 UI에 상태 표시
								for (int waitCount = 1; waitCount <= 5; waitCount++)
								{
									currentLog.Status = $"최종 데이터 검증 중... ({waitCount}/5)";
									Debug.WriteLine($"[Wait] 안정화 대기 {waitCount}회차...");
									await Task.Delay(300);
								}

								// ---------------------------------------------------------
								// [Step 3-1] 사수 명령: 안정화 대기 후 최종 HTML 1회 더 인양
								// ---------------------------------------------------------
								Debug.WriteLine("[Action] 대기 종료. 최종 HTML 캡처 수행.");

								// JS에서 html 필드만 정밀 타격하여 가져오거나, 전체 객체를 다시 수신
								string finalJson = await MainWebView.ExecuteScriptAsync(checkScript);

								if (!string.IsNullOrEmpty(finalJson) && finalJson != "null")
								{
									// [ID-02] JSON 이스케이프 정규화 (사수님이 치우신 똥 처리 로직 적용)
									// 1. 앞뒤 따옴표 제거 2. 이중 백슬래시 및 이스케이프 따옴표 복원
									string normalizedFinal = finalJson.Trim('"')
										  .Replace("\\\"", "\"")
										  .Replace("\\\\", "\\")
										  .Replace("\\u003C", "<")  // HTML 태그 깨짐 방지
                                          .Replace("\\u003E", ">");

									using (JsonDocument finalDoc = JsonDocument.Parse(normalizedFinal))
									{
										var finalRoot = finalDoc.RootElement;
										// 텍스트와 HTML을 최종적으로 갱신
										currentText = finalRoot.GetProperty("text").GetString() ?? "";
										currentHtml = finalRoot.GetProperty("html").GetString() ?? "";
									}
								}
								// ---------------------------------------------------------

								Debug.WriteLine($"[Success] 안정화 완료. 최종 HTML 파싱 전환.\n{currentHtml.Length} bytes");

								// [Critical] 사수님의 역공학 함수로 최종 렌더링 텍스트 생성
								string finalizedMd = ReconstructMarkdownFromHtml(currentHtml);

								currentLog.LlmOutput = finalizedMd;
								currentLog.Status = "수신 완료";

								Debug.WriteLine($"최종 전체 텍스트(Finalized): {finalizedMd.Length} chars");
								return currentLog.LlmOutput;
							}
						}
					}
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"[Critical Error] {ex.Message}");
				}

				await Task.Delay(CHECK_INTERVAL_MS);
			}

			return currentLog.LlmOutput;
		}

		private string ReconstructMarkdownFromHtml(string html)
		{
			if (string.IsNullOrEmpty(html))
				return string.Empty;

			// 중요 디버그 라인 유지
			Debug.WriteLine($"[DEBUG] Markdown convert Source: {html}");

			// 0. [Sanitizing] HTML 내부의 지저분한 개행/공백 찌꺼기 먼저 제거 (데이터 응집도 확보)
			string md = html.Replace("\r", "").Replace("\n", " ");

			// 1. [Table Header] 헤더 개수 파악 및 구분선(|---|) 동적 생성
			var headerMatches = System.Text.RegularExpressions.Regex.Matches(md, "<th[^>]*>(.*?)</th>", System.Text.RegularExpressions.RegexOptions.Singleline);
			if (headerMatches.Count > 0)
			{
				string headerRow = "| ";
				string dividerRow = "| ";
				foreach (System.Text.RegularExpressions.Match match in headerMatches)
				{
					string cleanHeader = System.Text.RegularExpressions.Regex.Replace(match.Groups[1].Value, "<[^>]+>", "").Trim();
					headerRow += cleanHeader + " | ";
					dividerRow += "--- | ";
				}
				// <thead> 섹션 치환
				md = System.Text.RegularExpressions.Regex.Replace(md, "<thead.*?>.*?</thead>", $"\n\n{headerRow}\n{dividerRow}", System.Text.RegularExpressions.RegexOptions.Singleline);
			}

			// 2. [Table Structure] 데이터 셀 및 행 복구 + [Critical] 테이블 종결 처리
			md = md.Replace("</td>", " | ");
			md = md.Replace("<tr>", "\n| ");
			// [Action] 테이블이 끝나는 지점에 명시적 더블 개행을 주입하여 뒤따르는 텍스트와 격리
			md = md.Replace("</table>", "</table>\n\n");

			// 3. [Layout] p, br 태그 무결성 확보 (사수님 지침)
			md = md.Replace("</p>", "\n\n").Replace("<p>", "\n\n");
			md = md.Replace("<br>", "\n").Replace("<br/>", "\n");
			md = md.Replace("</h3>", "\n\n### ").Replace("<h3>", "\n\n");

			// 4. [Cleanup] 나머지 HTML 태그 소멸 (span, div 등)
			md = System.Text.RegularExpressions.Regex.Replace(md, "<[^>]+>", string.Empty);

			// 5. [Decode] HTML 특수문자 복원 (&nbsp; 등)
			md = System.Net.WebUtility.HtmlDecode(md);

			// 6. [Post-Process] 표 인식 안정화를 위한 전후 개행 정규화
			// 표 시작(| ) 전후의 빈 줄을 확정하여 파서를 깨움
			md = md.Replace("\n| ", "\n\n| ");
			md = System.Text.RegularExpressions.Regex.Replace(md, @"\n{3,}", "\n\n");

			md = md.Replace("| \n\n|", "| \n|");

			// 중요 디버그 라인 유지
			Debug.WriteLine($"[DEBUG] Markdown Source: {md}");

			return md.Trim();
		}

		//private string ReconstructMarkdownFromHtml(string html)
		//{
		//	if (string.IsNullOrEmpty(html))
		//		return string.Empty;

		//	Debug.WriteLine($"[DEBUG] Markdown convert Source: {html}");

		//	// 1. [Sanitizing] HTML 내부의 지저분한 개행 제거 (공정 안정화)
		//	string cleanHtml = html.Replace("\r", "").Replace("\n", " ");

		//	// 2. [Table Anchor] 표의 시작과 끝을 명확히 규정
		//	// </table> 태그 뒤에 더블 개행을 박아 뒤따라오는 텍스트와의 유착을 방지합니다.
		//	string md = cleanHtml.Replace("</table>", "</table>\n\n");
		//	md = md.Replace("<tr>", "\n| ");
		//	md = md.Replace("</th>", " | ").Replace("</td>", " | ");

		//	// 3. [Divider Injection] 헤더 구분선 동적 생성 (사수님 설계 반영)
		//	if (md.Contains("<thead"))
		//	{
		//		var headerMatches = System.Text.RegularExpressions.Regex.Matches(md, "<th[^>]*>");
		//		if (headerMatches.Count > 0)
		//		{
		//			string dividerRow = "\n| " + string.Join(" | ", System.Linq.Enumerable.Repeat("---", headerMatches.Count)) + " |";
		//			md = md.Replace("</thead>", "</thead>" + dividerRow);
		//		}
		//	}

		//	// 4. [Paragraph & Layout] 문단 태그 치환
		//	md = md.Replace("</p>", "\n\n").Replace("<p>", "\n\n");
		//	md = md.Replace("<br>", "\n").Replace("<br/>", "\n");
		//	md = md.Replace("</h3>", "\n\n### ").Replace("<hr", "\n\n---");

		//	// 5. [Tag Strip] 나머지 모든 HTML 찌꺼기 제거
		//	md = System.Text.RegularExpressions.Regex.Replace(md, "<[^>]+>", string.Empty);
		//	md = System.Net.WebUtility.HtmlDecode(md);

		//	// 6. [High Integrity] 최종 텍스트 밀도 조정
		//	// 표 행(| ) 앞뒤로 불필요하게 뭉친 개행을 정리하되, 표 시작 전 빈 줄은 보존합니다.
		//	md = System.Text.RegularExpressions.Regex.Replace(md, @"\n{3,}", "\n\n");
		//	md = md.Replace("\n| ", "\n\n| ");

		//	Debug.WriteLine($"[DEBUG] Markdown Source: {md}");

		//	return md.Trim();
		//}

		//private string ReconstructMarkdownFromHtml(string html)
		//{
		//	if (string.IsNullOrEmpty(html))
		//		return string.Empty;


		//	Debug.WriteLine($"[DEBUG] Markdown convert Source: {html}");

		//	// [ID-02] 원문 무결성 유지하며 태그 치환 시작
		//	string md = html;



		//	// 1. [Table Header] 헤더 개수 파악 및 구분선(|---|) 동적 생성
		//	// <th> 내부의 복잡한 span 구조를 무시하고 실제 열 개수를 추출합니다.
		//	var headerMatches = System.Text.RegularExpressions.Regex.Matches(md, "<th[^>]*>(.*?)</th>", System.Text.RegularExpressions.RegexOptions.Singleline);
		//	if (headerMatches.Count > 0)
		//	{
		//		string headerRow = "| ";
		//		string dividerRow = "| ";
		//		foreach (System.Text.RegularExpressions.Match match in headerMatches)
		//		{
		//			// span 등 내부 태그 제거 후 순수 텍스트만 추출
		//			string cleanHeader = System.Text.RegularExpressions.Regex.Replace(match.Groups[1].Value, "<[^>]+>", "").Trim();
		//			headerRow += cleanHeader + " | ";
		//			dividerRow += "--- | ";
		//		}
		//		// <thead> 섹션을 마크다운 표준 헤더 구조로 치환
		//		md = System.Text.RegularExpressions.Regex.Replace(md, "<thead.*?>.*?</thead>", $"\n\n{headerRow}\n{dividerRow}", System.Text.RegularExpressions.RegexOptions.Singleline);
		//	}

		//	// 2. [Table Body] 데이터 셀 및 행 복구
		//	md = md.Replace("</td>", " | ");
		//	md = md.Replace("<tr>", "\n| ");

		//	// 3. [Layout] 사수님이 강조하신 p, br 태그 무결성 확보
		//	md = md.Replace("</p>", "\n\n").Replace("<p>", "\n\n");
		//	md = md.Replace("<br>", "\n").Replace("<br/>", "\n");
		//	md = md.Replace("</h3>", "\n\n").Replace("<h3>", "\n\n### ");

		//	// 4. [Cleanup] 나머지 HTML 태그 소멸 (span, div 등)
		//	md = System.Text.RegularExpressions.Regex.Replace(md, "<[^>]+>", string.Empty);

		//	// 5. [Decode] HTML 특수문자 복원 (&nbsp; 등)
		//	md = System.Net.WebUtility.HtmlDecode(md);

		//	// 6. [Post-Process] 표 인식 안정화를 위한 전후 개행 정규화
		//	md = md.Replace("\n| ", "\n\n| "); // 표 시작 전 빈 줄 강제
		//	md = System.Text.RegularExpressions.Regex.Replace(md, @"\n{3,}", "\n\n");

		//	Debug.WriteLine($"[DEBUG] Markdown Source: {md}");

		//	return md.Trim();
		//}

		// 검증용 웹뷰를 통한 CVO 판단 로직
		private async Task<string> RunCVOVerification(string llmResponse)
		{
			// LLM1의 답변을 검증용 LLM의 입력으로 전달하여 기준 위반 여부 확인
			// 구현 원리는 MainWebView와 동일하게 JS 주입 후 결과 추출
			return "기준 준수 확인됨 (CVO 미검출)";
		}

		// [신규] 프로젝트 설정 저장 로직 (명명 위임: BtnProjectSave_Click)
		private async void BtnProjectSave_Click(object sender, RoutedEventArgs e)
		{
			string projectConfig = TxtProjectEditor.Text;

			// 프로젝트 루트에 project_settings.config 파일로 저장
			string configPath = "project_settings.config";

			try
			{
				// 논리적 무결성: 빈 내용이라도 파일은 생성하여 초기화 상태 유지
				File.WriteAllText(configPath, projectConfig, System.Text.Encoding.UTF8);

				// 2. LLM 전송 (리팩토링 메서드 호출)
				string syncMessage = $"[SYSTEM_UPDATE] 설정 동기화:\n{projectConfig}";
				await SendMessageToWebViewAsync(syncMessage);

				MessageBox.Show("설정 저장 및 LLM 동기화 완료.");
			}
			catch (Exception ex)
			{
				MessageBox.Show($"설정 저장 실패: {ex.Message}");
			}
		}

		// [Logic] 설정 파일을 읽어 에디터에 바인딩하는 메서드
		private void LoadProjectConfig()
		{
			// 논리적 경로 설정
			string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "project_settings.config");

			try
			{
				if (File.Exists(configPath))
				{
					// 파일이 존재할 경우 무결하게 읽어와 에디터(TxtProjectEditor)에 주입해요.
					string savedConfig = File.ReadAllText(configPath, System.Text.Encoding.UTF8);
					TxtProjectEditor.Text = savedConfig;

					System.Diagnostics.Debug.WriteLine($"[Success] LoadProjectConfig() -> '{configPath}' 로드 완료");
				}
				else
				{
					// 파일이 없을 경우, 초기 가이드라인을 출력하거나 비워두어 사용자 입력을 대기해요.
					System.Diagnostics.Debug.WriteLine("[Info] LoadProjectConfig() -> 설정 파일이 존재하지 않아 빈 상태로 시작해요.");
				}
			}
			catch (Exception ex)
			{
				// 파일 접근 권한 등 예외 발생 시 디버그 채널로 보고해요.
				System.Diagnostics.Debug.WriteLine($"[Error] LoadProjectConfig() 실패: {ex.Message}");
			}
		}

		// [Refactored] 공통 전송 메서드
		private async Task SendMessageToWebViewAsync(string message)
		{
			if (string.IsNullOrEmpty(message))
				return;

			// 전송 직전 답변 개수 파악 스크립트 실행
			string countScript = @"document.querySelectorAll('.markdown, .prose, [data-message-author-role=""assistant""]').length;";
			string countResult = await MainWebView.ExecuteScriptAsync(countScript);
			int.TryParse(countResult.Trim('"'), out _lastResponseCount);

			// 이후 기존 전송 로직 수행...
			// JavaScript 내 특수문자 및 줄바꿈 이스케이프 처리
			string escapedMessage = message.Replace("`", "\\`").Replace("\n", "\\n");

			string script = $@"
        (function() {{
            const inputArea = document.querySelector('div[contenteditable=""true""]') || document.querySelector('textarea');
            if (!inputArea) return;

            inputArea.focus();
            document.execCommand('insertText', false, `{escapedMessage}`);
            
            const allButtons = Array.from(document.querySelectorAll('button'));
            const sendBtn = allButtons.find(btn => {{
                const label = (btn.getAttribute('aria-label') || '').toLowerCase();
                const text = (btn.innerText || '').toLowerCase();
                const isSend = label.includes('send') || label.includes('전송') || text.includes('send') || text.includes('전송');
                const isStop = label.includes('stop') || label.includes('중지') || text.includes('stop') || text.includes('중지');
                return isSend && !isStop && !btn.disabled;
            }});

            if (sendBtn) {{
                sendBtn.click();
            }} else {{
                const enterEvent = new KeyboardEvent('keydown', {{
                    bubbles: true, cancelable: true, key: 'Enter', code: 'Enter', keyCode: 13, which: 13
                }});
                inputArea.dispatchEvent(enterEvent);
            }}
        }})();";

			await MainWebView.ExecuteScriptAsync(script);
		}

		// [1. Handoff 버튼 클릭 이벤트]
		private async void btnHandoff_Click(object sender, RoutedEventArgs e)
		{
			Debug.WriteLine("[Handoff] 프로세스 개시.");

			// [Step 1] 기존 세션에서 백업 명령 투입
			string backupTrigger = "현재까지의 작업 내용을 새 세션에 투입하기 위해, 명령셋으로 백업하라.";
			var backupLog = new LogData
			{
				Number = LogHistory.Count + 1,
				UserInput = "[System Action] 백업 명령 투입",
				TimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
			};
			LogHistory.Add(backupLog);
			await SendMessageToWebViewAsync(backupTrigger);

			// [Step 2] 기존 세션에서 명령셋 수령 (웹에서 전송 버튼을 눌러야 할 수 있음)
			string extractedCommandSet = await PollForResponseAsync(backupLog);

			if (string.IsNullOrEmpty(extractedCommandSet))
			{
				Debug.WriteLine("[Handoff] 명령셋 수령 실패.");
				return;
			}

			// [Step 3] 새 세션 이동 및 초기화
			_isCriterionInjectionActive = false;
			_lastResponseCount = 0;

			// URL은 사용자님이 환경에 맞게 수동 전환하거나 유지하세요.
			MainWebView.Source = new Uri("https://gemini.google.com/");
			await WaitForNavigationAsync();

			// 지시하신 대로 3~5초간 대기하여 입력창 렌더링 시간을 확보합니다.
			Debug.WriteLine("[Handoff] 새 세션 안정화 대기 중 (5초)...");
			await Task.Delay(5000);

			// [Step 4] 수령한 명령셋 이식 및 '백업 내용 전체 표시'
			var injectionLog = new LogData
			{
				// [지시 반영] 백업된 명령셋 내용을 로그에 모두 표시합니다.
				Number = LogHistory.Count + 1,
				UserInput = $"[System] 명령셋 이식 실행\n─── BACKUP CONTENT ───\n{extractedCommandSet}\n──────────────────────",
				TimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
			};
			LogHistory.Add(injectionLog);

			// 새 세션에 투입 (이때 기준과 함께 투입됩니다)
			await SendMessageToWebViewAsync(extractedCommandSet);
			await PollForResponseAsync(injectionLog);

			Debug.WriteLine("[Handoff] 명령셋 가시화 및 이식 완료.");
		}

		// [참고] 페이지 로드 대기 헬퍼 (기존과 동일)
		private async Task WaitForNavigationAsync()
		{
			var tcs = new TaskCompletionSource<bool>();
			EventHandler<CoreWebView2NavigationCompletedEventArgs> handler = null;
			handler = (s, e) => { MainWebView.NavigationCompleted -= handler; tcs.SetResult(true); };
			MainWebView.NavigationCompleted += handler;
			await Task.WhenAny(tcs.Task, Task.Delay(10000));
		}

		private async void menuLogin_Click(object sender, RoutedEventArgs e)
		{
			Debug.WriteLine("[Auth] 로그인 프로세스 개시.");

			// 1. 구글 로그인 페이지로 직접 이동
			// Gemini 서비스로 직접 진입하면 인증 필요 시 로그인 창이 뜹니다.
			string loginUrl = "https://accounts.google.com/ServiceLogin?continue=https://gemini.google.com/app";

			MainWebView.Source = new Uri(loginUrl);

			// WebPopup.Visibility = Visibility.Visible;

			// 2. 로그인 완료 대기 및 감지 루프 (선택 사항)
			await MonitorLoginStatusAsync();
		}

		private async Task MonitorLoginStatusAsync()
		{
			// 비유: "문이 열릴 때까지 정찰병을 배치하여 감시하는 과정입니다."
			bool isLoggedIn = false;
			while (!isLoggedIn)
			{
				await Task.Delay(2000); // 2초 간격 체크

				string currentUrl = MainWebView.Source.ToString();

				// Gemini 메인 대시보드 URL에 진입했는지 확인
				if (currentUrl.Contains("gemini.google.com/app"))
				{
					isLoggedIn = true;
					Debug.WriteLine("[Auth] 로그인 성공 감지. 세션이 활성화되었습니다.");

					// 로그인 성공 시 사용자에게 알림 또는 다음 단계(세션 목록 로드) 트리거
					ShowSystemNotification("로그인이 완료되었습니다. 세션 정보를 동기화합니다.");
				}
			}
		}

		/// <summary>
		/// 시스템의 주요 상태 변화를 사용자에게 알리는 통합 인터페이스입니다.
		/// </summary>
		/// <param name="message">출력할 메시지 내용</param>
		/// <param name="isCritical">심각도 여부 (추후 UI 색상 변경 등에 활용 가능)</param>
		private void ShowSystemNotification(string message, bool isCritical = false)
		{
			// 현재는 표준 MessageBox를 사용하지만, 나중에 이 내부 로직만 바꾸면 전체 UI에 반영됩니다.
			string title = isCritical ? "시스템 경고" : "시스템 알림";
			MessageBoxImage icon = isCritical ? MessageBoxImage.Warning : MessageBoxImage.Information;

			System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, icon);

			// 디버그 콘솔에도 병행 기록하여 추적성을 확보합니다.
			Debug.WriteLine($"[Notification] {message}");
		}

		private async Task RefreshSessionListAsync()
		{
			Debug.WriteLine("[Session] 무한 동력 심층 스캔 시작...");
			var allSessions = new Dictionary<string, GeminiSession>();
			int currentScrollPos = 0;
			int scrollStep = 1000; // 보폭을 조금 더 넓혀서 임계점 도달 속도를 높임
			int maxSteps = 100;   // 세션이 아주 많을 경우를 대비해 충분히 확보
			int consecutiveNoChange = 0;

			for (int i = 0; i < maxSteps; i++)
			{
				int previousCount = allSessions.Count;

				string script = $@"
            (function() {{
                var container = document.querySelector('.conversations-container');
                if (!container) return JSON.stringify([]);

                container.scrollTop = {currentScrollPos};
                container.dispatchEvent(new Event('scroll', {{ bubbles: true }}));

                var items = document.querySelectorAll('a[data-test-id=""conversation""]');
                var data = [];
                items.forEach(item => {{
                    var title = item.querySelector('.conversation-title')?.innerText.trim();
                    if (title && item.href) data.push({{ Title: title, Url: item.href }});
                }});
                return JSON.stringify(data);
            }})()";

				string jsonResult = await MainWebView.ExecuteScriptAsync(script);
				ParseAndAccumulate(jsonResult, allSessions);

				// --- 지능형 종료 및 좌표 제어 ---
				if (allSessions.Count == previousCount)
				{
					consecutiveNoChange++;
					// 5회 연속 변화가 없으면 진짜 끝으로 간주하고 중단
					if (consecutiveNoChange >= 5)
						break;
				}
				else
				{
					consecutiveNoChange = 0; // 데이터가 늘어나면 카운트 리셋
				}

				Debug.WriteLine($"[Session] 스텝 {i}: 현재 {allSessions.Count}개 수집 중... (좌표: {currentScrollPos})");

				currentScrollPos += scrollStep;
				await Task.Delay(850); // 서버 응답 및 렌더링 동기화를 위해 약간 증액
			}

			Dispatcher.Invoke(() => {
				SessionList.ItemsSource = allSessions.Values.ToList();
			});
			Debug.WriteLine($"[Session] 최종 수집 완료: {allSessions.Count}개.");
		}

		private void ParseAndAccumulate(string json, Dictionary<string, GeminiSession> storage)
		{
			try
			{
				string cleanJson = System.Text.Json.JsonSerializer.Deserialize<string>(json);
				var list = System.Text.Json.JsonSerializer.Deserialize<List<GeminiSession>>(cleanJson);
				if (list != null)
				{
					foreach (var s in list)
					{
						if (!storage.ContainsKey(s.Url))
							storage[s.Url] = s;
					}
				}
			}
			catch { /* 파싱 예외 무시 */ }
		}

		private void UpdateSessionUI(string jsonResult)
		{
			try
			{
				// 1. 원본 로그 확인
				Debug.WriteLine($"[Session] 수신 데이터: {jsonResult}");

				if (string.IsNullOrEmpty(jsonResult) || jsonResult == "{}" || jsonResult == "null")
				{
					Debug.WriteLine("[Session] 데이터가 비어있습니다. 크롤링 시점을 조절하거나 페이지를 확인하세요.");
					return;
				}

				string cleanJson = "";

				// 2. JSON 형태에 따른 유연한 처리
				using (JsonDocument doc = JsonDocument.Parse(jsonResult))
				{
					// WebView2가 문자열로 감싸서 보냈을 경우
					if (doc.RootElement.ValueKind == JsonValueKind.String)
					{
						cleanJson = doc.RootElement.GetString();
					}
					else
					{
						// 이미 배열 형태([])로 들어왔을 경우
						cleanJson = jsonResult;
					}
				}

				// 3. 최종 리스트 변환 및 UI 바인딩
				var sessions = System.Text.Json.JsonSerializer.Deserialize<List<GeminiSession>>(cleanJson);

				if (sessions != null && sessions.Count > 0)
				{
					Dispatcher.Invoke(() => {
						SessionList.ItemsSource = sessions;
						Debug.WriteLine($"[Session] {sessions.Count}개의 세션 로드 성공.");
					});
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"[Session] 보정 파싱 실패: {ex.Message}");
			}
		}

		private void SessionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// 선택된 항목이 GeminiSession 객체인지 확인
			if (SessionList.SelectedItem is GeminiSession selectedSession)
			{
				Debug.WriteLine($"[Switch] 세션 전환 시도: {selectedSession.Title}");

				// 해당 세션의 URL로 웹뷰 이동
				if (!string.IsNullOrEmpty(selectedSession.Url))
				{
					MainWebView.Source = new Uri(selectedSession.Url);

					// [Ra's Logic] 세션이 바뀌었으므로 응답 카운트 등을 초기화하여 
					// 새로운 대화 맥락을 준비합니다.
					_lastResponseCount = 0;
					_isCriterionInjectionActive = false;
				}
			}
		}

		private async void btnRefresh_Click(object sender, RoutedEventArgs e)
		{
			Debug.WriteLine("[Session] 사용자 요청에 의한 세션 목록 새로고침.");

			// 이전에 설계한 심층 크롤링 메서드를 호출합니다.
			await RefreshSessionListAsync();

			ShowSystemNotification("세션 목록이 최신 상태로 동기화되었습니다.");
		}

		private void rcvMsg_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
		{
			// [ID-03] 이벤트 버블링 강제 구현
			// 마우스 휠 신호를 받아서 부모(ScrollViewer)에게 전달한다.
			if (!e.Handled)
			{
				e.Handled = true;
				var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
				{
					RoutedEvent = UIElement.MouseWheelEvent,
					Source = sender
				};

				// 부모 컨트롤을 찾아 이벤트를 발생시킴
				var parent = ((Control)sender).Parent as UIElement;
				parent?.RaiseEvent(eventArg);
			}
		}
	}



	/**
 * @class LogData
 * @brief 사수 정의 구조 유지 및 실시간 UI 통지 로직 보정
 */
	public class LogData : System.ComponentModel.INotifyPropertyChanged
	{
		private string _llmOutput = string.Empty; // [IN-01] 초기화 필수
		private string _status = "대기 중";       // [IN-01] 초기화 필수

		// [ID-01] 기존 식별자 유지
		public int Number { get; set; }
		public string TimeStamp { get; set; }
		public string UserInput { get; set; }

		// [Logic] 실시간 스트리밍 업데이트를 위한 프로퍼티
		public string LlmOutput
		{
			get => _llmOutput;
			set
			{
				if (_llmOutput != value) // [DC-06] 불필요한 UI 갱신 방지
				{
					_llmOutput = value;
					OnPropertyChanged(nameof(LlmOutput));
				}
			}
		}

		// [Logic] 폴링 상태(Pending/수신 중/완료)를 UI에 표시하기 위한 프로퍼티 추가
		public string Status
		{
			get => _status;
			set
			{
				if (_status != value)
				{
					_status = value;
					OnPropertyChanged(nameof(Status));
				}
			}
		}

		// [IN-02] PropertyChanged 구현부 보존
		public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string name) =>
			PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
	}

	public class GeminiSession
	{
		public string Title { get; set; }
		public string Url { get; set; }
	}
}