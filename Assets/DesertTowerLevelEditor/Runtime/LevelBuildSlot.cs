using System;
using System.Collections.Generic;
using UnityEngine;

namespace DesertTower.Levels
{
    /// <summary>레벨에 미리 정해 둔 건설 자리다. 플레이 중 점유 정보는 별도로 관리한다.</summary>
    [DisallowMultipleComponent]
    public sealed class LevelBuildSlot : MonoBehaviour
    {
        [Tooltip("저장과 건설 요청에 쓰는 고유 ID. 복제한 슬롯은 새 ID로 바꾸세요.")]
        public string id = Guid.NewGuid().ToString("N");
        public string label = "건설 슬롯";
        [Tooltip("같은 바닥의 자유 배치와 슬롯은 같은 ID를 사용합니다.")]
        public string occupancySurfaceId;
        [Tooltip("이 슬롯에 지을 수 있는 시설 종류 ID. 비어 있으면 건설할 수 없습니다.")]
        public List<string> allowedFacilityIds = new List<string>();
        [Tooltip("슬롯 자리의 가로와 세로 크기. 시설의 실제 충돌 검사는 별도로 합니다.")]
        public Vector2 footprint = new Vector2(2, 2);
        public bool available = true;

        /// <summary>빠진 ID나 잘못된 크기를 확인한다.</summary>
        public bool TryValidate(out string error)
        {
            if (!ValidData(id, occupancySurfaceId, footprint, allowedFacilityIds, out error)) return false;
            if (!Finite(transform.position) || !Finite(transform.rotation.eulerAngles)
                || transform.lossyScale != Vector3.one)
            { error = "슬롯 위치와 회전을 확인하고 Scale을 (1,1,1)로 맞추세요."; return false; }
            return true;
        }
        public static bool ValidData(string id, string surfaceId, Vector2 size,
            IList<string> allowed, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(surfaceId))
                error = "슬롯 ID와 바닥 ID가 필요합니다.";
            else if (!Finite(new Vector3(size.x, 0, size.y)) || size.x <= 0 || size.y <= 0)
                error = "슬롯 크기는 유한한 양수여야 합니다.";
            else if (allowed == null) error = "허용 시설 목록이 필요합니다.";
            else
            {
                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (var value in allowed)
                    if (string.IsNullOrWhiteSpace(value) || !ids.Add(value))
                    { error = "허용 시설 목록에 빈 ID나 중복 ID가 있습니다."; break; }
            }
            return error == null;
        }
        public static bool Finite(Vector3 p)
            => !float.IsNaN(p.x) && !float.IsInfinity(p.x)
                && !float.IsNaN(p.y) && !float.IsInfinity(p.y)
                && !float.IsNaN(p.z) && !float.IsInfinity(p.z);
    }
}
