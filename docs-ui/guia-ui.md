Para garantir a consistência do produto "Fundação da Plataforma", aqui está o guia de estilos (Style Guide) baseado no conceito Cyber-Technical aplicado nas telas:

1. Paleta de Cores (Cores Sólidas e Efeitos)
O esquema de cores é focado em alto contraste sobre fundo escuro, simulando interfaces de terminais avançados e centros de comando.

Background Primário: #0D0F12 (Preto azulado profundo)
Background Secundário (Cards/Sidebar): #161B22
Ação/Destaque (Neon): #39FF14 (Verde Neon/Cyber Green) - Usado para botões principais e estados de sucesso.
Alerta/Erro: #FF3131 (Vermelho Neon) - Usado para falhas e estados críticos.
Bordas/Divisores: #30363D (Cinza metálico discreto)
Texto Primário: #E6EDF3 (Branco levemente acinzentado)
Texto Secundário: #8B949E (Cinza para metadados)
2. Tipografia
A tipografia mistura legibilidade moderna com a estética técnica de código.

Títulos e Interface: Inter ou Roboto (Sans-serif) para clareza em comandos e menus.
Dados e Status: JetBrains Mono ou Fira Code (Monospaced) - Essencial para exibir URLs de repositórios, branches, IDs de solicitação e etapas de execução (ex: ANALISANDO_SEGURANCA).
3. Componentes de UI
Botões: Cantos vivos ou levemente arredondados (4px). O botão principal possui um efeito de glow sutil quando ativo.
Cards: Fundo #161B22 com bordas finas de 1px. Sem sombras projetadas, utilizando apenas diferenciação de cor de fundo.
Status Badges: Pílulas com fundo semi-transparente e borda na cor do status (Verde para CONCLUÍDA, Vermelho para FALHOU, Azul para EM_EXECUÇÃO).
4. Elementos Visuais "Cyber"
Grid Background: Um padrão de grade (dots ou linhas finas) muito sutil no fundo das páginas de cadastro.
Scanning Effect: Uma linha horizontal animada (opcional) ou gradientes lineares que dão a sensação de "leitura" em cards de análise ativa.
Ícones: Linhas finas (Lucide React ou Phosphor Icons), preferencialmente em estilo duotone ou com cores sólidas neon.
5. Comportamento (UX)
Feedback Imediato: Validação de URL de repositório (HTTP/HTTPS) com feedback visual instantâneo (Check verde ou X vermelho).
Polling Visual: Indicadores de carregamento (spinners) estilizados como "bússolas" ou radares técnicos durante a mudança de etapas.