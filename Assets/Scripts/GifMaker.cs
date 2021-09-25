using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class GifMaker : MonoBehaviour
{

    public Image img;
    public string folderName;
    public string headText;
    public int imageLength;
    public bool IsFirstNum1;
    public bool EnableSkipFirst;
    private bool Flg = true;
    private int firstFrameNum;
    private int _firstFrameNum;

    void Start()
    {

        Application.targetFrameRate = 60;

        if (IsFirstNum1)
        {
            _firstFrameNum = 1;
        }
        else
        {
            _firstFrameNum = 0;
        }

        if (EnableSkipFirst)
        {
            _firstFrameNum = _firstFrameNum + 1;
        }

        firstFrameNum = _firstFrameNum;
    }

    void Update()
    {
        if (!Flg) return;

        //最後の画像を超えたら　最初に戻る
        firstFrameNum++;
        if (firstFrameNum > imageLength) firstFrameNum = _firstFrameNum;

        string _fixedFirstFrameNum = (firstFrameNum).ToString();
        if (firstFrameNum < 10)
        {
            _fixedFirstFrameNum = "0" + _fixedFirstFrameNum;
        }

        //画像変更
        Sprite tex = Resources.Load<Sprite>(folderName + "/" + headText + _fixedFirstFrameNum);
        img.sprite = tex;

        //一周したら停止
        if (firstFrameNum == imageLength)
        {
            //SetFlg(false);
        }
    }

    public void SetFlg(bool _flg)
    {
        if (_flg == true)
        {
            firstFrameNum = _firstFrameNum;
            Sprite tex;
            if (IsFirstNum1)
            {
                if (EnableSkipFirst)
                {
                    tex = Resources.Load<Sprite>(folderName + "/" + headText + "02");
                }
                else
                {
                    tex = Resources.Load<Sprite>(folderName + "/" + headText + "01");
                }
            }
            else
            {
                if (EnableSkipFirst)
                {
                    tex = Resources.Load<Sprite>(folderName + "/" + headText + "01");
                }
                else
                {
                    tex = Resources.Load<Sprite>(folderName + "/" + headText + "00");
                }
            }

            img.sprite = tex;
        }

        Flg = _flg;
    }

    public bool GetFlg()
    {
        return Flg;
    }
}