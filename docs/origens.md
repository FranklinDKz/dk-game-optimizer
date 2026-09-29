# Arquivos de referência e ações incorporadas

As ações abaixo foram reimplementadas no aplicativo. Os arquivos `.bat` e `.reg` da pasta de referência não são executados diretamente: cada comando é fixo, aparece na prévia e tem tratamento de erro próprio.

| Arquivo de referência | Ação no aplicativo |
| --- | --- |
| `2 - .Bats/CPU/cpu máximo desempenho.bat` | Plano Alto desempenho na aba Windows |
| `3 - Regedits/Desativar Game DVR 1.reg` e `2.reg` | Desativar Game DVR com cópia dos valores anteriores |
| `2 - .Bats/Windows/Desativar efeitos visuais.bat` | Perfil visual e animação, com restauração |
| `2 - .Bats/Windows/Desativar Transparência do Windows.bat` | Transparência, com restauração |
| `2 - .Bats/Memória RAM/Otimizar Memory Management.bat` | `LargeSystemCache=0`, com restauração |
| `7 - Otimizar Driver de Vídeo/AMD e NVIDIA/Desativar HGS.reg` | Ajuste opcional de HAGS, com restauração |
| `7 - Otimizar Driver de Vídeo/AMD, NVIDIA e INTEL/Desativar MPO` | Ajuste opcional de MPO, com restauração |
| `2 - .Bats/SSD/Habilitar TRIM.bat` | Consulta o estado antes de habilitar TRIM, com restauração |
| `7 - Otimizar Driver de Vídeo/NVIDIA/Limpar Shader Cache NVIDIA.bat` | Limpeza com prévia e idade mínima; também inclui caches AMD |
| `6 - Otimizar Rede (ping)/Resetar Rede (use no fim).bat` | Apenas `ipconfig /flushdns` como manutenção pontual |
| `9 - Otimização Semanal/Arrumar Windows.bat` | DISM e SFC sob demanda, com log |
| `1 - Criar Ponto de Restauração` | `Checkpoint-Computer` sob demanda |
| `8 - Otimizações especificas de jogos/*.bat` | Prioridade Alta apenas no processo selecionado e em execução, com restauração |

Os arquivos de prioridade para GTA V e Call of Duty apontam, em alguns casos, para o executável do Fortnite. A versão integrada confere o caminho escolhido antes de alterar um processo. O script de Red Dead Redemption 2 também escrevia um `Engine.ini` do Fortnite; nenhuma configuração de jogo é sobrescrita pelo aplicativo.

Não foram incluídos os comandos que desativam UAC, SmartScreen, Defender, Windows Update, serviços essenciais ou Hyper-V, os que mudam o temporizador de boot e os arquivos de ativação de Windows/Office. As regras de RAM por quantidade alteravam o limite de separação de serviços sem medir carga real; o aplicativo mantém o gerenciamento de memória do Windows. A limpeza forçada de RAM também não foi incluída porque remove cache que os jogos e o sistema podem reutilizar.
