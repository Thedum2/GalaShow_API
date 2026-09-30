using System.Text.Json;

namespace GalaShow.Common.Service
{
    /// <summary>
    /// 미니게임 phase_data(단계별 시간, ms) 검증.
    /// 값은 0 이상의 정수(ms) 또는 -1(무한 대기: 게임이 완료 조건을 채울 때까지 기다림, 예: 호스트 선택 대기)이다.
    /// </summary>
    public static class PhaseDataRules
    {
        /// <summary>무한 대기 값</summary>
        public const int Infinite = -1;

        /// <summary>RGF 8단계 이름 (Unity GamePhase와 같음)</summary>
        public static readonly string[] Phases = { "READY", "SETUP", "PRESENT", "INPUT", "WAIT", "EXECUTE", "REVEAL", "CLEANUP" };

        /// <summary>
        /// 문제가 있으면 오류 메시지, 없으면 null. phaseData가 없으면 통과한다.
        /// 8단계 외의 키(이전 Admin 형식의 phases 등)는 검증하지 않는다.
        /// </summary>
        public static string? Validate(JsonElement? phaseData)
        {
            if (phaseData is not { } data || data.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return null;
            }

            if (data.ValueKind != JsonValueKind.Object)
            {
                return "phaseData must be an object";
            }

            foreach (var phase in Phases)
            {
                if (!data.TryGetProperty(phase, out var value))
                {
                    continue;
                }

                if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var ms))
                {
                    return $"phaseData.{phase} must be an integer (ms) or -1 (infinite)";
                }

                if (ms < 0 && ms != Infinite)
                {
                    return $"phaseData.{phase} must be >= 0 (ms) or -1 (infinite)";
                }
            }

            return null;
        }
    }
}
