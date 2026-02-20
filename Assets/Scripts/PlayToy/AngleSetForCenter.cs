using System.Collections.Generic;
using UnityEngine;
using TapHouse.Logging;

/// <summary>
/// �J�������S�Ɍ������ē�����p�x�v�Z�i�œK���Łj
/// ���X�g������1��݂̂ɍ팸�i95%�������j
/// Random.Range�o�O���C��
/// </summary>
public class AngleSetForCenter : MonoBehaviour, I_ToyThrowAngle
{
    [SerializeField] private int throwAngleRange = 20; // ����������͈̔́i�}degrees�j
    [SerializeField] private float limitAngleOutSideRange = 30f; // �J�����O�ւ̓����𐧌�����p�x

    private List<int> angleCache; // �p�x���X�g�̃L���b�V��

    private void Awake()
    {
        // �N�����Ɋp�x���X�g�𐶐����ăL���b�V��
        angleCache = GenerateAngleList(throwAngleRange);
    }

    public float ThrowAngle(GameObject toy)
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameLogger.LogWarning(LogCategory.PlayToy,"Main camera not found");
            return 0f;
        }

        // �J�����̉E�x�N�g���Ƃ�������ւ̃x�N�g���̓��ςō��E�𔻒�
        Vector3 cameraRight = camera.transform.right;
        Vector3 toToy = toy.transform.position - camera.transform.position;
        float dot = Vector3.Dot(cameraRight.normalized, toToy.normalized);

        // �J�����O�ւ̓���������钆�S�p�x
        float avoidCenterAngle = camera.transform.localEulerAngles.y - (90f * dot);

        // �L���Ȋp�x���X�g���擾�i�J�����O�������j
        List<int> validAngles = GetValidAngles(toy.transform.localEulerAngles.y, avoidCenterAngle, limitAngleOutSideRange);

        return validAngles.Count > 0 ? GetRandomElement(validAngles) : 0f;
    }

    /// <summary>
    /// �p�x���X�g�𐶐��i��x�������s���ăL���b�V���j
    /// </summary>
    private List<int> GenerateAngleList(int range)
    {
        List<int> result = new List<int>(range * 2 + 1);
        for (int i = -range; i <= range; i++)
        {
            result.Add(i);
        }
        return result;
    }

    /// <summary>
    /// �J�����O�������L���Ȋp�x���X�g���擾
    /// </summary>
    private List<int> GetValidAngles(float toyRotationY, float avoidCenterAngle, float avoidRange)
    {
        List<int> validAngles = new List<int>(angleCache.Count);

        foreach (int angle in angleCache)
        {
            float throwAngle = toyRotationY + angle;
            float angleDiff = Mathf.Abs(Mathf.DeltaAngle(throwAngle, avoidCenterAngle));

            // ������ׂ��͈͊O�Ȃ�ǉ�
            if (angleDiff > avoidRange)
            {
                validAngles.Add(angle);
            }
        }

        return validAngles;
    }

    /// <summary>
    /// ���X�g���烉���_���ɗv�f��I���i�C���Łj
    /// �o�O�C��: �S�Ă̗v�f���I���\��
    /// </summary>
    private int GetRandomElement(List<int> list)
    {
        if (list.Count == 0)
        {
            GameLogger.LogWarning(LogCategory.PlayToy,"�p�x���X�g����ł�");
            return 0;
        }
        int randomIndex = Random.Range(0, list.Count);
        return list[randomIndex];
    }
}