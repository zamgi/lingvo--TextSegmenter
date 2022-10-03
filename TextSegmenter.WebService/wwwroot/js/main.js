$(document).ready(function () {
    var MAX_INPUTTEXT_LENGTH  = 10000,
        LOCALSTORAGE_TEXT_KEY = 'ts-text', LOCALSTORAGE_LANG_KEY = 'ts-lang',
        DEFAULT_TEXT =
'бабушкакозликаоченьлюбила\r\n' +
'баб_ушкакозликаоченьлюбила\r\n' +
'вротебатьебатькопать\r\n' +
'Itiseasytoreadwordswithoutspaces\r\n' +
'fuckingtextsegmentation\r\n' +
'Исправляемопечаткисучётомконтекста\r\n' +
'dasistfantastisch\r\n' +
'MayerwandtesichentschlossendemNationalsozialismuszu.AlsprominenterVertreterderMediävistikwollteerzurgeistigenMobilmachungbeitragenunddieRelevanzderhistorischenForschungfürdasneuzuschaffendeEuropabeweisen\r\n' +
'MayersZielwardieErarbeitungeineseuropäischenGeschichtsbildes,dasvorallemvonderdeutschenGeschichtswissenschaftausbestimmtwird\r\n' +
'глокаякуздраштекобудланулабокраикурдячитбокрёнка\r\n' +
'ВаркалосьХливкиешорькиПырялисьпонавеИхрюкотализелюкиКакмюмзикивмове\r\n' +
'\r\n' +
'Недавномнепонадобиласьбиблиотекадляисправленияопечаток\r\n' +
'Большинствооткрытыхспеллчекеров(кпримеруhunspell)неучитываютконтекст\r\n' +
'абезнегосложнополучитьхорошуюточность\r\n' +
'ЯвзялзаосновуспеллчекерПитераНорвига\r\n' +
'EsisteinfachWörterohneLeerzeichenzulesen\r\n' +
'прикрутилкнемуязыковуюмодель(набазеNграмм)\r\n' +
'ускорилего(используяподходSymSpell)\r\n' +
'NuclearDNAisanucleicacidapolymericbiomoleculeorbiopolymerfoundinthenucleusofeukaryoticcells\r\n' +
'поборолсильноепотреблениепамяти(черезbloomfilterиperfecthash)\r\n' +
'thewesternyellowrobinisaspeciesofbirdintheaustralasianrobinfamilynativetoaustralia\r\n' +
'азатемоформилвсёэтоввидебиблиотекинаC++соswigбиндингамидлядругихязыков\r\n' +
'InseinenAnfangsjahrentratermitwirtschaftsundsiedlungsgeschichtlichenArbeitenhervor\r\n' +
            'азатемоформилвсёэтоввидебиблиотекинаC++соswigбиндингамидлядругихязыковазатемоформилвсёэтоввидебиблиотекинаC++соswigбиндингамидлядругихязыковазатемоформилвсёэтоввидебиблиотекинаC++соswigбиндингамидлядругихязыковазатемоформилвсёэтоввидебиблиотекинаC++соswigбиндингамидлядругихязыков';

    var textOnChange = function () {
        var _len = $('#text').val().length; 
        var len = _len.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ' ');
        var $textLength = $('#textLength');
        $textLength.html('length of text: ' + len + ' characters');
        if (MAX_INPUTTEXT_LENGTH < _len) $textLength.addClass('max-inputtext-length');
        else                             $textLength.removeClass('max-inputtext-length');
    };
    var getText = function( $text ) {
        var text = trim_text( $text.val().toString() );
        if (is_text_empty(text)) {
            alert('Enter the text to be processed.');
            $text.focus();
            return (null);
        }
        
        if (text.length > MAX_INPUTTEXT_LENGTH) {
            if (!confirm('Exceeded the recommended limit ' + MAX_INPUTTEXT_LENGTH + '  characters (on the ' + (text.length - MAX_INPUTTEXT_LENGTH) + ' characters).\r\nText will be truncated, continue?')) {
                return (null);
            }
            text = text.substr(0, MAX_INPUTTEXT_LENGTH);
            $text.val(text);
            $text.change();
        }
        return (text);
    };

    $('#text').focus(textOnChange).change(textOnChange).keydown(textOnChange).keyup(textOnChange).select(textOnChange).focus();

    (function () {
        function isGooglebot() { return (navigator.userAgent.toLowerCase().indexOf('googlebot/') !== -1); };
        if (isGooglebot()) return;

        var text = localStorage.getItem(LOCALSTORAGE_TEXT_KEY);
        if (!text || !text.length) text = DEFAULT_TEXT;
        $('#text').val(text).focus();
        $('#lang').val(localStorage.getItem(LOCALSTORAGE_LANG_KEY));
    })();
    $('#resetText2Default').click(function () {
        $('#text').val('');
        setTimeout(function () { $('#text').val(DEFAULT_TEXT).focus(); }, 100);
    });

    $('#processButton').click(function () {
        if($(this).hasClass('disabled')) return (false);

        var lang = $('#lang').val();
        var text = getText( $('#text') );
        if (!text) return (false);

        processing_start();
        if (text !== DEFAULT_TEXT) {
            localStorage.setItem(LOCALSTORAGE_TEXT_KEY, text);
        } else {
            localStorage.removeItem(LOCALSTORAGE_TEXT_KEY);
        }
        localStorage.setItem(LOCALSTORAGE_LANG_KEY, lang);

        var model = {
            text: text,
            lang: lang
        };
        $.ajax({
            type       : 'POST',
            contentType: 'application/json',
            dataType   : 'json',
            url        : '/Process/Run',
            data       : JSON.stringify( model ),
            success: function (responce) {
                if (responce.err) {
                    processing_end();
                    $('.result-info').addClass('error').text(responce.err);
                } else {
                    if (responce.ttps && responce.ttps.length) {
                        $('.result-info').removeClass('error').text('');
                        var rows = ['<tr><td>#</td><td>original text</td><td>language</td><td>result</td></tr>'];
                        for (var i = 0, len = responce.ttps.length; i < len; i++) {
                            var x = responce.ttps[i];
                            var z = '';
                            for (var j = 0, k = x.tps.length; j < k; j++) {
                                z += x.tps[j].term + ' ';
                            }
                            rows.push('<tr><td>' + (i + 1) + '</td><td>' + wrap_text(x.text, 50) + '</td><td class=' + x.lang + '>' + x.lang + '</td><td>' + z + '</td></tr>');
                        }
                        $('#processResult tbody').html(rows.join(''));
                        processing_end();
                        $('.result-info').hide();
                    } else {
                        processing_end();
                        $('.result-info').text('text segmentation is not defined');
                    }
                }
            },
            error: function () {
                processing_end();
                $('.result-info').text('ошибка сервера');
            }
        });
    });

    function processing_start(){
        $('#text').addClass('no-change').attr('readonly', 'readonly').attr('disabled', 'disabled');
        $('.result-info').show().removeClass('error').html('Processing... <label id="processingTickLabel"></label>');
        $('#processButton').addClass('disabled');
        $('#processResult tbody').empty();
        processingTickCount = 1; setTimeout(processing_tick, 1000);
    };
    function processing_end(){
        $('#text').removeClass('no-change').removeAttr('readonly').removeAttr('disabled');
        $('.result-info').removeClass('error').text('');
        $('#processButton').removeClass('disabled');
    };
    function trim_text(text) { return (text.replace(/(^\s+)|(\s+$)/g, '')); };
    function is_text_empty(text) { return (!trim_text(text)); };
    function wrap_text(text, wrap_length) {
        var c = parseInt(text.length / wrap_length);
        if (0 < c) {
            var a = [], s, rem = text.length % wrap_length;
            for (var i = 0; i < c; i++) {
                s = text.substr(i * wrap_length, wrap_length) + '<wbr/>';
                a.push( s );
            }
            s = text.substr(text.length - rem, rem);
            a.push( s );
            return (a.join(''));
        }
        return (text);
    };
    //(function() {
    //    $.ajax({
    //        type       : 'POST',
    //        contentType: 'application/json',
    //        dataType   : 'json',
    //        url        : '/Process/Run',
    //        data       : JSON.stringify( { text: '_dummy_\r\n_думмиблять_' } )
    //    });
    //})();

    var processingTickCount,
        processing_tick = function () {
            var n2 = function (n) {
                n = n.toString();
                return ((n.length === 1) ? ('0' + n) : n);
            }
            var d = new Date(new Date(new Date(new Date().setHours(0)).setMinutes(0)).setSeconds(processingTickCount));
            var t = n2(d.getHours()) + ':' + n2(d.getMinutes()) + ':' + n2(d.getSeconds()); //d.toLocaleTimeString();
            var $s = $('#processingTickLabel');
            if ($s.length) {
                $s.text(t);
                processingTickCount++;
                setTimeout(processing_tick, 1000);
            } else {
                processingTickCount = 1;
            }
        };
});
