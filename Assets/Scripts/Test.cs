using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class Test : MonoBehaviour {

    private void Start()
    {
        reverseString();
    }

    List<int> m_emptyIndexList = new List<int>();

    StringBuilder m_testText = new StringBuilder("Focused, hard work is the real key to success. Keep your eyes on the goal, and just keep taking the next step towards completing it.");


    void ChapterReverse(int _startIndex,int _endInex,StringBuilder _stringbuilder)
    {
        StringBuilder tempStringBuilder = _stringbuilder;
        char tempChar;
        int tempEmptyCount = 0;

        //先移除空格
        for (int i = _startIndex; i < _endInex - m_emptyIndexList.Count; i++)
        {
            if (tempStringBuilder[i] == ' ')
            {
                tempStringBuilder.Remove(i,1);
                m_emptyIndexList.Add(i);
                ++tempEmptyCount;
            }
        }

        //单句翻转
        int tempEndIndex = _endInex - tempEmptyCount;
        for (int j = 0; j < (tempEndIndex - _startIndex + 1) / 2; j++)
        {
            tempChar = tempStringBuilder[j + _startIndex];
            tempStringBuilder[j + _startIndex] = tempStringBuilder[tempEndIndex - j];
            tempStringBuilder[tempEndIndex - j] = tempChar;
        }

        //插入空格
        for (int i = 0; i < m_emptyIndexList.Count; i++)
        {
            int tempIndex = _endInex - tempEmptyCount;
            //插入时候没移除标点符号，+1
            tempStringBuilder.Insert(tempIndex - m_emptyIndexList[i] + _startIndex + 1, ' ');
        }
        m_emptyIndexList.Clear();

        //单句中的单词翻转
        int tempWordStartIndex = -1;
        int tempWordEndIndex = 0;
        for (int i = _startIndex; i <= _endInex; i++)
        {
            tempChar = tempStringBuilder[i];
            if (tempWordStartIndex == -1 && tempChar != ' ')
            {
                tempWordStartIndex = i;
            }
            else if (tempChar == ' ' || i == _endInex)
            {
                tempWordEndIndex = (tempChar == ' ') ? (i - 1) : (_endInex);
                for (int j = 0; j < (tempWordEndIndex - tempWordStartIndex + 1)/2; j++)
                {
                    tempChar = tempStringBuilder[j + tempWordStartIndex];
                    tempStringBuilder[j + tempWordStartIndex] = tempStringBuilder[tempWordEndIndex - j];
                    tempStringBuilder[tempWordEndIndex - j] = tempChar;
                }
                tempWordStartIndex = -1;
            }
           
        }
        Debug.Log(tempStringBuilder);
    }

    void reverseString()
    {
        char tempChar;
        int tempStrtIndex = 0;
        for (int i = 0; i < m_testText.Length; i++)
        {
            tempChar = m_testText[i];
            if (tempChar == ',' || tempChar == '.')
            {
                ChapterReverse(tempStrtIndex, i - 1, m_testText);
                if (i + 1 < m_testText.Length)
                {
                    if (m_testText[i + 1] == ' ')
                    {
                        tempStrtIndex = i + 2;
                        i += 1;
                    }
                    else
                    {
                        tempStrtIndex = i + 1;
                    }
                }
            }
        }
    }
}
