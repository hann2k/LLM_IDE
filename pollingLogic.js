/* [L-01] 초정밀 실측 로직: 웹뷰 콘솔 및 C# 로그 동시 출력 모드 */
(function (userInputRaw) {
    const trimAll = (s) => s ? s.replace(/[\s\n\r\t\u00A0\u200B\ufeff]/g, '').trim() : '';
    const inputVal = trimAll(userInputRaw);
    const containers = document.querySelectorAll('div.conversation-container');

    // [L-01] 사수 명령: 웹뷰 콘솔에 비교 기준 즉시 노출
    console.log("[Matching Start] Target:", inputVal);
    console.log("[Containers Found]:", containers);

    let logMsg = `\n[System Matching Target (Full)]\n"${userInputRaw}"\n`;
    logMsg += `[Normalized Target (Full)]\n"${inputVal}"\n`;

    let result = { text: '', isFinished: false, isValidBlock: false, log: '' };

    for (let j = containers.length - 1; j >= 0; j--) {
        const container = containers[j];
        const userQuery = container.querySelector('user-query');
        if (!userQuery) continue;

        const nodeRaw = userQuery.innerText;

        // [D-01] 구분자 강제 절단
        const targetPart = nodeRaw.includes('[USER_INPUT]') ? nodeRaw.split('[USER_INPUT]')[1] : nodeRaw;
        const targetClean = trimAll(targetPart);
        const isMatch = (targetClean === inputVal);

        // [L-01] 웹뷰 콘솔 실측 보고
        console.log(`[Index ${j}] Match: ${isMatch}`);
        console.log(`   ㄴ NodeRaw:`, nodeRaw);
        console.log(`   ㄴ TargetClean:`, targetClean);

        // [L-01] C# 전달용 로그 누적 (생략 없이 전체)
        logMsg += `--------------------------------------------------\n`;
        logMsg += `[Container Index: ${j}] Match Result: ${isMatch}\n`;
        logMsg += `[Node Raw Data (Full)]\n${nodeRaw}\n`;
        logMsg += `[Target Clean Data (Full)]\n${targetClean}\n`;

        if (isMatch) {
            const resp = container.querySelector('model-response');
            if (resp) {
                result.isValidBlock = true;
                result.text = resp.querySelector('message-content')?.innerText || '';
                result.isFinished = !!resp.querySelector('message-actions');

                console.log(`[FOUND] Match at Index ${j}. isFinished: ${result.isFinished}`);
                logMsg += `>> [MATCHED] 컨테이너 ${j}에서 일치 항목 발견. 수신 상태: ${result.isFinished}\n`;
                break;
            }
        }
    }
    result.log = logMsg;
    return JSON.stringify(result);
})(/*DATA_INJECTION*/);