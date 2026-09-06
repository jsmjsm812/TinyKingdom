using System;
using System.Collections.Generic;

namespace WitchHour.Core
{
    /// <summary>
    /// GuardianData/ItemData 에셋은 JSON에 직접 못 담아서(오브젝트 참조) GuardianRegistry와 같은
    /// 이유로 에셋 파일명(assetName)만 저장하고, 불러올 때 레지스트리로 되짚는다.
    /// </summary>
    [Serializable]
    public class SavedGuardianPlacement
    {
        public string guardianAssetName;
        public int slotIndex;
        public int starLevel;
    }

    [Serializable]
    public class SavedItemCount
    {
        public string itemAssetName;
        public int count;
    }

    /// <summary>
    /// 배틀씬 "그만하기" 버튼으로 저장하는 구역별 진행 상태 전부 — 웨이브 번호뿐 아니라 골드,
    /// 산 아이템, 필드에 배치된 수호자(슬롯·성급)까지 통째로 담는다. JsonUtility는 배열 안의
    /// null 원소를 제대로 못 다뤄서(역직렬화하면 null이 아니라 빈 객체가 됨) "체크포인트 없음"은
    /// null 대신 hasCheckpoint=false로 표현한다 — GameProgress.Checkpoints는 항상 3개의
    /// non-null 인스턴스를 들고 있다.
    /// </summary>
    [Serializable]
    public class ZoneCheckpoint
    {
        public bool hasCheckpoint;
        public int wave;
        public int gold;
        public List<SavedGuardianPlacement> guardians = new List<SavedGuardianPlacement>();
        public List<SavedItemCount> items = new List<SavedItemCount>();
    }
}
