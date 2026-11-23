using UnityEngine;

namespace DungeonDLC.Sorny
{
    public sealed class SornyTrainingDummy : MonoBehaviour
    {
        public Transform corpo;
        public AudioClip impacto;
        AudioSource fonte;
        float oscilacao;
        Quaternion repouso;

        void Awake()
        {
            if (corpo) repouso = corpo.localRotation;
            fonte = gameObject.AddComponent<AudioSource>(); fonte.playOnAwake = false;
            fonte.spatialBlend = 1; fonte.dopplerLevel = 0; fonte.minDistance = 1; fonte.maxDistance = 10;
        }

        public void ReceberGolpe(bool pesado)
        {
            oscilacao = pesado ? 1 : .65f;
            var chegada = FindFirstObjectByType<SornyArrival>(); if (chegada) chegada.RegistrarAcerto(pesado);
            if (fonte && impacto) { fonte.volume = SornySession.Efeitos * .8f; fonte.pitch = pesado ? .8f : 1; fonte.PlayOneShot(impacto); }
        }

        void LateUpdate()
        {
            oscilacao = Mathf.MoveTowards(oscilacao, 0, Time.deltaTime * 1.7f);
            if (corpo) corpo.localRotation = repouso * Quaternion.Euler(Mathf.Sin(Time.time * 24) * oscilacao * 9, 0, Mathf.Sin(Time.time * 18) * oscilacao * 5);
        }
    }
}
