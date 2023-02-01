<H2>更新的详细说明请参见项目 <b>TFS/info.txt</b></H2>
<br/>

<H2>BLADE.TCPFORTRESS   TFS堡垒  .Net4.8 C#  WindowsService Sqlserver</H2>
<br/><br/>

简介：
<br/><br/>
本项目是一套带有黑白名单和自动防护功能的tcp端口防护程序。其需求设计基于本人使用云主机以来的经验总结。
由于众多的云主机长期被嗅探，被暴力试探。而主机商能提供的免费防火墙和安全策略是非常有限的，且操作和管理非常耗时间和精力。
本人首先设置限制策略，将数十台主机的管理端口集中收缩至两台中心管理主机。由管理主机端口转发至目标主机管理端口。
因为涉及多种操作系统，且每台云主机的带宽和性能也不高，如果采用强制集中管理远程连接会严重影响工作效率。所以仅采用端口转发。
但在日常使用中，集中管理主机的诸多端口还是会被长期扫描和嗅探。虽然不断更新系统防火墙黑名单的工作效率提高了不少，但依旧很烦人。
堡垒机是个不错的解决方案，但是显然的，商用堡垒机的功能成熟，但是价格不菲，且需要的硬件配置较高。
所以便根据本人的实际需求出发，设计编写了这套防护程序。

需求简述：<br/>
    1：针对RDP、SSH、SQL等tcp端口进行防护，限制有威胁的远端IP接入。<br/>
    2：有黑白名单管理功能，有运行时自动灰名单管理能力（基于时间段连接次数判断）。<br/>
    3：不分析连接协议，名单判定和网络转发的性能尽可能保证高效，对每个连接可以设置最大限速。<br/>
    4：可设置限制连接次数、封锁时间、封锁策略等细节信息。<br/>
    5：数据存于网络数据库，多套堡垒程序可以共用黑白名单数据。<br/>
    6：基于 windows服务 实现。<br/>
    7：根据配置信息决定运行时日志记录的详细程度。<br/>
<br/><br/>

现状：<br/>
    经过一段时间的使用，程序的性能和稳定性已经得到了验证，功能细节上也经过了优化调整。<br/>
    目前仅针对Tcp端口提供服务，限制有威胁的IP地址连接。本人目前没有太多UDP协议使用经验，所以现在没有涉及UDP管理策略，不排除以后可以添加。<br/>
    封锁目标可以是 IP地址，IP段，包括Ipv4和IPv6。 黑白名单均可使用IP段设置，运行时灰名单只能是IP地址。<br/>
    配置程序界面很丑，我知道很丑，也略显杂乱。但是没办法，本人基本只做后端服务，确实非常不擅长前端界面的设计工作。但是服务本身的性能还是相当不错的。<br/>
<br/><br/>
应用场景举例：<br/>
    01：云主机两台作为管理机（两台是为了互备，管理机可以单个也可以多台）A：111.111.111.111  B：222.222.222.222  设置这两台主机本身的RDP端口为11111 <br/>
    02：在主机商防火墙或系统防火墙放开管理机的TCP端口 例如 9000-9100。<br/>
    03：云主机数台，为需要防护管理的业务机（管理机本身也可以是业务机，管理机自己的远程管理端口同样可以被转发保护）。设置远程管理端口为 RDP/SSH：11111 。 <br/>
    04：业务机通过防火墙限制仅允许 IP：111.111.111.111 和 222.222.222.222 访问 管理端口11111。<br/>
    05：安装并配置本项目TFS（包括安装windows服务和数据库部署）。<br/>
    06：在管理机上规划分配端口给业务机。例如9001给业务机Z的11111端口，9002给业务机X的11111端口等。记得给本机分配转发端口，例如将9100端口给管理本机的11111端口。<br/>
    07：以后即可通过111.111.111.111的相应端口访问对应的业务机管理端口。例如 111.111.111.111:9002 即可访问业务机X的 11111端口。<br/>
    08：管理机防火墙禁止外网访问 11111端口。（以后需要通过9100端口访问管理机的管理端口）<br/>
    09：当有IP地址访问受管端口，TFS堡垒立即记录下IP，时间，计数。根据黑或白名单的设置信息，放行或禁止连接。对于未配置的IP地址，将根据时间和计数条件触发灰名单记录并拦截。<br/>
    10：记录到数据库的灰名单IP，可以自动记录为黑名单，以长期封禁。也可以存为灰名单，以便管理员分析是否赦免，或归类IP地址段封禁。<br/>
    11：多台管理机可以使用相同的数据库，则黑白灰名单数据是共用的。而各自的封禁数值和配置策略可以不同，分别由本机的Settings.cfg文件确定。<br/>
    **：如果使用单台管理机，当更新程序服务停止时，本机管理端口也会失去连接，造成无法继续操作的问题。只能通过防火墙暂时放开远程端口处理，或使用其他允许访问的主机中转操作。所以两台管理机更方便。<br/>

<br/>
项目安装和配置：<br/>
    1：在管理机上安装 .Net v4.8 ，复制本项目安装包到所需位置，再安装本项目服务程序（ 以管理员方式运行Cmd，进入安装包目录，运行 01.InstallService.bat ）。<br/>
    2：编辑项目目录中的 Settings.cfg 文件(xml文档格式)。如安装目录中没有此文件，则可以通过运行 BLADE.TCPFORTRESS.SetApp.exe 自动生成一个基本配置。<br/>
    3：项目中 BLADE.TCPFORTRESS.SetApp.exe 为图形化的配置程序。 BLADE.TCPFORTRESS.TFSERVICE.exe 为堡垒服务本身。 TFS.mdf 为数据库文件，附加至SQLserver即可使用。<br/>
    4：需要在SQLserver中重新配置用户和权限，并生成sql访问字符串，写入配置文件。<br/>
    5：启动服务，或重启主机以自动启动服务。<br/>
    6：默认日志信息保存在项目安装目录下的logs目录，也可以通过配置指定其他路径。<br/>
<br/><br/>

配置程序介绍（pic1图片注释）：<br/>
    01：加载配置文件。系统服务是默认加载Settings.cfg文件。配置程序可以编辑其他备份文件作为预置。并自动将文件解析至右侧编辑区。<br/>
    02：加载文件后，xml文件显示在此区域。可以手工编辑此处。编辑后，可点击03按钮，将文件解析到右侧。<br/>
    03：将xml文件解析到右侧设置区。一般用于手工修改xml文件后，解析到右侧，顺便检查是否有书写错误。<br/>
    04：编码右侧设置区的内容到左侧xml文件。用于保存准备。<br/>
    05：保存配置文件。此处保存文件即是覆盖01步加载的文件。<br/>
    06：显示黑白名单编辑界面。（需加载配置文件后此按钮可用。所以在修改过数据库连接串后，请保存并重新加载配置文件后，再打开名单编辑，以免操作错误数据库。）<br/>
    07：堡垒服务的基本配置部分。<br/>
         Debug：是否启用debug模式，此模式主要影响日志的输出详细程度，用于分析运行状况。<br/>
         LockGray：接入IP地址重复触发限制后，IP将计入灰名单，并封锁一定时间。选择此项，即为长久封锁，除非服务重启，否则不再放行此IP。反之则超过封禁时间后恢复放行并重新计数。<br/>
         AutoBlack：触发灰名单后存入数据库时，是否直接存为黑名单。选择是，则自动记录为黑名单。反之则记录为灰名单，为管理员日后分析所用。<br/>
         WOB：选择服务以 白名单、灰名单、黑名单模式运行。 白名单模式仅放行白名单IP地址或IP段，对其他地址进行灰名单记录，但直接拒绝连接。灰名单模式按照计数和时间自动封禁。黑名单模式先检查黑名单，如发现记录则直接封禁，如未发现，则应用灰名单规则。<br/>
         Lock Second：单位秒。触发灰名单规则后，封锁的时长。如LockGray为真，则长久封禁。<br/>
         CountTime：连接计数的时间，单位为秒。设定时间内连接次数超限则进入灰名单封禁。<br/>
         Count：暂时弃用。设定时间内连接限制次数现在由每个转发器通道Tun分别设置。<br/>
         LogPath：日志文件路径，可以是其他完整路径，但请确认指定位置的文件操作权限。<br/><br/>
    08：转发通道Tun List的配置区。<br/>
         Add Tun：增加一个Tun转发通道。<br/>
         Delete：删除这个Tun。<br/>
         Enable Rule：此Tun是否应用服务设定的规则模式WOB。不选择即直通，不进行封锁判定。<br/>
         In Address / Port：侦听的地址和端口。地址设置为0.0.0.0则表示侦听全部网络。多网卡也可以填写特定IP地址。<br/>
         Out Address / Port：转发出去的地址和端口。目前仅支持IP，暂不支持域名解析。<br/>
         LockCount：连接计数限制。在服务配置的计数时间内，连接次数超出此值则触发灰名单动作。请注意本服务并不解析通信内容，不能判断之前的连接是否成功。例如RDP远程桌面连接失败也会计数，多次尝试连接会触发会名单动作。补：RDP协议并非单一TCP连接，已知情况是单次登录至少使用两次Tcp连接，请考虑适当的限制值。<br/>
         Speed KB：单连接的限制带宽，单位KB。注：考虑性能需要，速度限制为粗略控制，发现超速后会适当降速，非严格速度检查。当客户端与主机之间目前的可用带宽远大于此处限制速度时，实际传输速度还是可以超过限制的。此限速仅在带宽紧张，连接数量多的时候平衡争抢带宽的情况。<br/><br/>
    11：赦免地址列表。可勾选删除，可添加。最多500个地址。赦免地址为减少误触发影响业务所设计。每小时执行一次灰名单赦免，将列表中的地址从灰名单中去除。如果录入地址为域名，则进行DNS解析，赦免所得到的所有IP地址。也可以在下方输入域名查询IP地址，避免解析域名的性能开销。注：不会解除黑名单的封锁。<br/>
    12：灰名单操作区<br/>
         LoadGary：刷新灰名单列表。以下大多编辑动作都会自动刷新灰名单列表。<br/>
         Set to BLACK / WHITE：将勾选的灰名单项目转存为黑、白名单。<br/>
         Delete IT：删除勾选的灰名单项目。<br/>
         表格区：显示灰名单项目的详细信息。灰名单项目不自动分析是否为IPV6。黄绿色条目为发现的近似地址，以便管理员封禁地址段。绿色背景的条目为相似掩码为16，黄色背景表示相似掩码为24或32。<br/><br/>
    13：黑白名单Tab列表，可删除条目。条目的选择为选定行，可多选。同上，会有颜色提示近似地址条目。<br/>
    15：Merge合并勾选项。在灰名单中勾选一个或多个项目（一般是近似地址项）则可以点击Merge Selected以尝试合并地址或地址段到下方。例如勾选 110.2.2.3 和 110.2.5.4 则可合并为 110.2.0.0/16地址段。如只选择一个地址项，则合并结果为原地址。也可以手动修改合并结果。<br/>
    16：Merge to Black / White 将上一步的合并结果存入黑/白名单。当然也可以直接手动填写合并结果Address，再存入黑/白名单，实现名单的手工录入。（添加黑名单的同时，会对合并结果的来源灰名单项目进行清除，条件是ID号，不会错删）<br/>
    17：测试区。可以输入一个地址段，和一个IP地址，进行包含测试。当然此处的意义主要是测试非8、16、24、32掩码的情况。用于管理员手动录入前的验证工作。<br/><br/>





