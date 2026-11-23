# Crediton — o Deus dos Juros

Boss final proposto para Dungeon & DLC, depois do Crack BandidoCulto.
Armas: Lâminas do Saldo Negativo — duas espadas de metal em forma de cifrão, presas por correntes.

Visual: guerreiro careca grotesco e propositalmente tosco, olho esbugalhado, barba quadrada, ombreira de moeda e couro vinho. Sprites frontais para inimigo 2D voltado à câmera em cenário 3D.

## Conteúdo

8 folhas PNG RGBA com transparência; 64 quadros; 10 sequências de animação.
Os PNGs foram preservados sem recorte, redimensionamento ou retoque por código. As folhas mais largas têm espaço extra para as armas.

## Ordem das animações

Os números abaixo começam em 1: leia os quatro quadros de cima da esquerda para a direita, depois os quatro de baixo.

| Animação | Arquivo (pasta Sprites) | Quadros | FPS inicial | Loop |
|---|---|---|---:|---|
| Idle | 01_idle_e_caminhada.png | 1–4 | 5 | Sim |
| Caminhada | 01_idle_e_caminhada.png | 5–8 | 7 | Sim |
| CobrancaDupla | 02_combo_cobranca_dupla.png | 1–8 | 10 | Não |
| Hit | 03_hit_e_atordoamento.png | 1–4 | 12 | Não |
| Atordoamento | 03_hit_e_atordoamento.png | 5–8 | 6 | Não |
| ExecucaoFiscal | 04_especial_execucao_fiscal.png | 1–8 | 8 | Não |
| JurosRotativos | 05_especial_juros_rotativos.png | 1–8 | 10 | Não |
| QuebraDeBanca | 06_especial_quebra_de_banca.png | 1–8 | 8 | Não |
| CaloteDivino | 07_transicao_fase2_calote_divino.png | 1–8 | 7 | Não |
| Falencia | 08_morte_falencia.png | 1–8 | 7 | Não |

## Importação na Unity

1. Extraia o ZIP e arraste a pasta Sprites para Assets.
2. Selecione os PNGs. Use Texture Type: Sprite (2D and UI), Sprite Mode: Multiple, Filter Mode: Point e Compression: None. Mantenha Alpha Is Transparency ativado e evite reduzir a textura pelo Max Size.
3. No Sprite Editor, use Slice > Grid By Cell Count: 4 colunas e 2 linhas. Use o retângulo completo das células; não use recorte automático por contorno para definir a ordem.
4. Se o fatiamento por contagem não distribuir os pixels de sobra corretamente, use os retângulos inteiros do JSON. Ele inclui coordenadas com origem superior esquerda e com origem inferior esquerda da Unity.
5. Crie os clips usando os quadros indicados na tabela. Ative Loop Time somente em Idle e Caminhada. O último quadro de Falencia é o cadáver e deve permanecer visível.
6. Confira os pivôs e a escala com a primeira pose de guarda de cada folha. A tabela abaixo oferece pontos de partida para compensar os tamanhos diferentes das folhas.

| Folha | Dimensão real | Grade | PPU inicial | Pivô inicial (X, Y) |
|---|---|---|---:|---|
| 01_idle_e_caminhada | 1536 × 1024 | 4 × 2 | 128 | (0.5, 0.1) |
| 02_combo_cobranca_dupla | 1536 × 1024 | 4 × 2 | 128 | (0.5, 0.1) |
| 03_hit_e_atordoamento | 1536 × 1024 | 4 × 2 | 128 | (0.5, 0.1) |
| 04_especial_execucao_fiscal | 1774 × 887 | 4 × 2 | 118 | (0.5, 0.169) |
| 05_especial_juros_rotativos | 1774 × 887 | 4 × 2 | 117 | (0.5, 0.183) |
| 06_especial_quebra_de_banca | 1536 × 1024 | 4 × 2 | 128 | (0.5, 0.1) |
| 07_transicao_fase2_calote_divino | 1536 × 1024 | 4 × 2 | 128 | (0.5, 0.1) |
| 08_morte_falencia | 1774 × 887 | 4 × 2 | 122 | (0.5, 0.135) |

As dimensões reais prevalecem sobre as dimensões pedidas nos prompts. Algumas folhas não têm largura ou altura divisível exatamente pela grade; o JSON distribui os pixels de sobra em retângulos inteiros.

## Movimento e dano

- Caminhada é um ciclo no lugar. O deslocamento pelo cenário precisa ser feito pelo controlador do inimigo.
- CobrancaDupla: dois impactos sugeridos, nos quadros 4 e 6.
- ExecucaoFiscal: impacto sugerido no quadro 5; as lâminas são arremessadas nas correntes e recolhidas.
- JurosRotativos: janela sugerida nos quadros 4 a 6. Isso não significa três danos automáticos; use uma regra de acerto por execução do golpe.
- QuebraDeBanca: impacto sugerido no quadro 6. É preciso sincronizar a área de dano com a aterrissagem.
- CaloteDivino: transição para segunda fase. A mudança de regras da luta precisa ser feita no controlador; a animação termina em guarda para reutilizar os outros clips.
- Hit e Atordoamento são sequências separadas dentro da mesma folha. Evite reiniciar Hit a cada contato se isso impedir o chefe de atacar.

## Verificação e limites

Conferidos: oito imagens RGBA, presença de fundo transparente, oito células não vazias por folha, ausência de pixels com alfa acima de 128 tocando as bordas das células e integridade do ZIP.
As poses foram revisadas visualmente. Estas são folhas de sprites geradas por IA; há pequenas variações entre desenhos, altura dos pés e alinhamento. O acabamento de animação exige conferir reprodução e ajustar pivôs, escala e tempo na Unity. Os valores deste guia são sugestões, não calibração feita no jogo.
Os clips, colisões, IA, eventos de dano e transições ainda não foram implementados ou testados no seu projeto Unity. O pacote contém os recursos visuais e o mapa das sequências.

## Arquivos auxiliares

animacoes_e_recortes.json: dimensões reais, retângulos de cada quadro, parâmetros iniciais e mapa dos clips.
prompts_de_geracao.json: especificações usadas para gerar as folhas e corrigir o espaço das armas.
As imagens foram produzidas pela geração de imagens integrada ao ChatGPT.
