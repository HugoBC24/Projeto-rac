using System;
using UnityEngine;

namespace ProjectRac.UI
{
    public enum CharacterSlot
    {
        None = 0,
        Left = 1,
        Center = 2,
        Right = 3
    }

    [Serializable]
    public class VisualNovelDialogueLine
    {
        [Tooltip("Nome do personagem exibido na plaquinha acima da caixa. Deixe vazio para ocultar a plaquinha.")]
        public string speakerName = "";

        public Color speakerNameColor = Color.white;

        [TextArea(3, 6)]
        public string text = "";

        [Tooltip("Sprite do personagem exibido na tela durante esta fala.")]
        public Sprite characterSprite;

        [Tooltip("Posicao na tela onde o sprite do personagem aparecera.")]
        public CharacterSlot slot = CharacterSlot.Right;

        [Tooltip("Se marcado, mantem os outros personagens ja visiveis na tela (escurecidos enquanto nao falam).")]
        public bool keepOtherCharacters = false;

        [Tooltip("Espelha o sprite horizontalmente.")]
        public bool flipHorizontal = false;

        [Tooltip("Clipe de audio opcional tocado ao iniciar esta fala.")]
        public AudioClip voiceClip;
    }
}
