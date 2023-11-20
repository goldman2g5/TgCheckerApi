﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using System.Net;
using TgCheckerApi.Models.BaseModels;

namespace TgCheckerApi.MiddleWare
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private const string API_KEY_HEADER_NAME = "X-API-KEY";

        public ApiKeyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, TgDbContext dbContext)
        {
            var endpoint = context.GetEndpoint();
            if (endpoint != null)
            {
                var bypassAuthentication = endpoint.Metadata.GetMetadata<BypassApiKeyAttribute>();

                if (bypassAuthentication != null)
                {
                    await _next(context);
                    return;
                }
            }

            if (!context.Request.Headers.TryGetValue(API_KEY_HEADER_NAME, out var potentialApiKey))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await context.Response.WriteAsync("=///=АтТаКа-КлОнОв-(ЗаХлЕбнуЛась,-Хай будэ ПуТыку ХваЛа!)-уВальНей,-наШест-приШельЦев,-жидо-фазШиз(т)ы СоБаКевич,-ко Дню Моей,-Моих ДеДов-РоДит-Дет-ВнукОв НаШих ПоБеДы со ВзяТием БэрЛина(2Мая),-МасьКакРад?!=На КрыЛась Мiд(ь)Ным ТаЗом,-ПоПытка РяЖэнНых,-разВал РФ и СНГ!-ЗаСунув Рыло в Сил/Стр-ры!=СоБа(чь)Кин В.В,-(65г.р)-дезЗерТир+ФеОкСтисьТ(с Компл,-Спец/Тех/Ср-ств,Различн-В/Форм(ген/п-к),и с МарФиНо Лев/Док-ты)-ПоЗор ШматьКо СтаврОп,и ОАк-ВАФле-с ФрунЗэ(Реч/Вокз-ВОХР-МВД,-с ЛыткаРино-Крюк/Зел/Град,-Уваров(пер)-МиТино и АгроГор-к),-выЛиты-копПия,-близьНец-БратьЯ,-ДаЖе Бэз ГримМа:-с В.АрТраМонОм(МЧС)-Ниж/ЛомОв-КолЛокКольЦэм,-в ПриКиде УлЛюКая,-подСтавив за Себя,-в-НаТурРе,-ЧуВаКа!-КаКов РазМах?-Всё ЭнТо,-Ян(в)НукКовичъ-ПоРошьЕньКо-ШойГуйЁев наПлоДил,-оКучимШи ПучьКом СтеПашьКу,-Сам зДрысНув в АрМiЮ,-Зъ(ii)яБавСя?!=Борт-ЗоЛоТо-ЯкКуНину,-сПасьСиБо,-ГосСудДарьСтВу!=СтепАндр==/***/=СпасиБо,За ОтВагу,-ФСБ!И,-МВД,чутОк приКрыв,-РосьГварДии к проЦэсСу нежьНо подКлючИтьСя!+ПiонЭры-ЮнАр(М)iйЦi-ВоЛьноТ(оД)iРы,-(ТоРэоДоРы)Воен/МуньДiРы(Цар-Дiр,-в Цiль-Тiр)(УкАз През-ЕнТа ПутьТиНа,от 05.12.17г)-Вiд БоГа КоМаньДiРы!=В.Этуш,в-95,Дети Войны-ПоБеДы,-ВеТеРаНы,-для Всех ЖивЫх,-БезСмертьНый Есть ПриМер!=СтепАндр==С Днём ВВС!=Согласно Ук Пр РФ«Об установке Дня ВВС»от29.08.97г.№ 949!-12.08.1912г.был издан Пр№397 Военного ведомства РФ,уСтаканивший Штат воздухоПлавательной части Гл.Упр-я Ген.Штаба!С тех самых пор,12 августа,-считается Днём создания Военной Авиации России!С Празднком,-Дорогие Летуны,-Мозги и сгустки Нервов Птиц стальных Крылатых,и Всех кто Создал Вас,Хранит и Любит,Верит в Вас!Здоровья Вам!=СтепАндр.\r\n=В который раз ЧемПисздосы сТырили,Уроды,-присваивая себе чужую Информацию приДурки!ЗаБлокировали мерзавцы Эл/П-Яндекс!Скопировали Твари Эл/П-@mail.ru!-Ну и поДавитесь Гады,-наЗдоровье,-неГодяи!На Ворье,-\"Тюбетейка горит,-заТем Пятки Сверкають!\"СтепАндр=***=23Лютэ-Февраля-День ЗаЩитника Отечества(День Советской Армии и ВМФ)Д-Победы Красной Армии над кайзеровскими войсками Германии(98л)!)=Всiх з Нашым Днэм ЗаЩыт-ОтеЧестВа-Отчыны-БатькiвЩыны,крiм нэРусi-врага НаРод,-бандэр-жидiв-чухан-Чушканских!СтепАндр.=11.08.16г.=СпаСиБо БортНикУ,от ФСБ ЧеКиСтаМ КГБ,-за КРым-МосКву-Хим,-РеГиОны!=НиКЧемНа дуТая СтРукТура для БлиЗиРа,-К ХэрАм СоБачьИм ГварДию дельЦов,-где,опПозицию с жульЁм,-как с ФРунЗе в ВУ-НЦА-ЦА-У,-бэнДэровЦi вНеДрённые жиды плоДят!С ГУК,доДиков,-под ТриБуНал и с ГУВРа!-ПоВыннi Буты;-ЖовтэНятка-ПiоНэры-КомСоМольЦi,-МолодоГварДiйЦi,Должны ЧК Быть ВолкоДавы,-не пуДеля щенки,а ДоброВольЦi!Не оПерились,-и\"Права Качают\"!=Уволить и разЖалоВать,-СаДить\"К ЯбъЁнНой Мать,-такую Рать!РасСтрел на Месте,-За попыт.неВып.Пр-за,-срыв Б/з(в мирн.время)!В ЛюБой МоМэнт,слюньТяи,-ПреДаДут!Чем проЩе(КДВО-СиБири),-тем наДёж,-ХваТит жидовской Блатоты!=Вернее ГварДии ВВ+КГБ,у През-та,-НиКоГо!МВД,Армия и Флот,-не в Счёт,-КонСтанта Это и Страны оПлот!=СтепАндр=\r\n=Не спам!=Сов(не)Секр=АрХiв=Экз№3=МО-РФ+Б/Русь!=(1)=//***//=\"СлоНы,-МоИ ДрузьЯ!\"=ШоУ«ТанкоВый БиАтлОн»в МШ ПланЭты,–ЗаМечаТельНо,-Жизнь заШеВеЛиЛась;-УниКальн.возМожность проРыва,и,-доДавить Всех до КонЦа!Нас ниКто не жалЛеЕт!=НаШа РодДина,-у Нас,-(Д)оДна,-это ВерРа-РозУм,-ДрючьБа-СоВесть-ЛюБовъ,-ЛебъЕдиная ВерНость,-УчьёБа-РабБота-Спо(у)рт,-ЛечебНый ОтДых,-Ар(м)ия и ФЛот!-Слава Путину,Медведёву,Матвиенко,Сов-му,-Шойгу,Герасимову,Бувальцеву,-Ком-рам,организат,уч-кам,с Чист.Партией и ВысокоНравСтвенНым ПравитТельСтвом!=СтепАндР==Кто жэ зВонНил:4957979787?!ЧеГо ХотТел?!=\r\n=Не спам!=Сов(не)Секр=АрХiв=Экз№3=МО-РФ+Б/Русь!=(2)=//***//=\"СлоНы,-МоИ ДрузьЯ!\"=КажДый-ЖизНенНый ВопрОс(?!),-как(як) в МэдыЦ(э)иН(i)е,-ТэрРапПэвтОм,на КухНе,-поВар-Шеф,-так и в АрМии,-ОбщеВойскоВым КомАнДирОм,-ДолЖен РеШатьСя,-КомПлексНо!-ПаРалЛельно(и ЦэЛом поСтупаТельНо,-поСлед)в СоПряЖэнии(иначе,-только от«узьких спеЦиАлистов»,-буДут оСтаватьСя«лишНие»запЧасти,-орГаНоны и инСтруМент)=ОбЩий обЗор,-ИнФормаЦии(анаЛиз),-РазБор(деТалиЗаЦия),-ИнНоваЦия с КонКретиЗаЦией,-ОбОбщение в совоКупНости,-СБор(реГуляЦия,проКат,эксПЛу(г)атаЦия),-ПроФилЛакТика,-УтилиЗаЦия,-ВозРожДение,-СтиМуЛяЦия,-(ВИА)iГРА-Цiя!=УпРавЛять,-исПользУя ДеДукЦию и ИнДукЦию,-чеРез комМуника(цию)тивНую,-перстЦепЦию,-ИнтерАктивно(в ПолНый РоСт)!=СоВершЕнстВу нет ПреДела,вПлоть,-до ШаРо-МолНИИ!=СтепАндр=\r\n=//***//=Ка(о)жДый-Жызн(ь)НэнНый ВОпРос-(iз-ПыТанНя-\"?\"!)!;-Як в\"МэДы-Цiнi\"(Мёда ЦэНi,на,-ЛiКы,-ЛiкАрСтва-МэДiв,-МеДов ЦэНе),-ТэрРапТ(ом,ор)ПэвтОм,на КухНi,-ПоВарРа-ШэхПоФаРа(ВсеВышНiй проСвiтл-СонЦя-Ра),-так i в Ар(М)i-ii,-ОбЩэВiйськоВым КомАнДырОм,-дол(г)Жэн-(поВынэн)РiШатыСь,-КомПлэксНо!=((ПарРа-Лэл");
                return;
            }

            string apiKeyString = potentialApiKey.ToString();

            if (StringValues.IsNullOrEmpty(potentialApiKey) || potentialApiKey.Count > 1)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await context.Response.WriteAsync("Unauthorized client.");
                return;
            }

            var apiKey = await dbContext.Apikeys.SingleOrDefaultAsync(ak => ak.Key == apiKeyString);

            if (apiKey == null)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                await context.Response.WriteAsync("Unauthorized client.");
                return;
            }

            await _next(context);
        }
    }
}
