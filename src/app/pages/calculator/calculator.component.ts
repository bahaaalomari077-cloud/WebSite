// @ts-nocheck
import { Component, ViewEncapsulation, AfterViewInit, OnDestroy, inject, effect } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LanguageService } from '../../services/language.service';
import { SeoService } from '../../services/seo.service';

@Component({
  selector: 'app-calculator',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './calculator.component.html',
  styleUrl: './calculator.component.scss',
  encapsulation: ViewEncapsulation.None
})
export class CalculatorComponent implements AfterViewInit, OnDestroy {
  lang = inject(LanguageService);
  currentLang = this.lang.currentLang;
  private _setLang: any;
  private _selMode: any;
  private _ddC: any;
  private _dpoC: any;
  private _stop: any;

  constructor() {
    inject(SeoService).set({
      title:       'Supply Chain Finance Calculator',
      description: 'Calculate the benefits of Dynamic Discounting and DPO extension for your business using the Credit Plus SCF calculator.',
      keywords:    'SCF calculator, dynamic discounting, DPO extension, finance calculator, Jordan'
    });
    effect(() => {
      const l = this.lang.currentLang();
      if (this._setLang) { this._setLang(l); }
    });
  }

  ngAfterViewInit(): void { this.initCalc(); }
  ngOnDestroy(): void { if (this._stop) { this._stop(); } }

  setLang(lang: string): void { if (this._setLang) { this._setLang(lang); } }
  selMode(mode: string): void { if (this._selMode) { this._selMode(mode); } }
  ddC(): void { if (this._ddC) { this._ddC(); } }
  dpoC(): void { if (this._dpoC) { this._dpoC(); } }

  private initCalc(): void {
    var __alive = true;
    var __self = this;
    __self._stop = function(){ __alive = false; };
    /* Calculator strings are loaded from app/i18n/en.ts and app/i18n/ar.ts via LanguageService */


    /* ============== CALCULATOR LOGIC ============== */
    var dds=0,dpos=0,curLang='en';
    var N=function(n){return Math.round(n).toLocaleString('en-US');};
    var N2=function(n){return parseFloat(n.toFixed(2)).toLocaleString('en-US',{minimumFractionDigits:2,maximumFractionDigits:2});};
    var P1=function(n){return(n*100).toFixed(1)+'%';};
    var P2=function(n){return(n*100).toFixed(2)+'%';};
    function ddV(){return{F:+document.getElementById('dd-F').value,d:+document.getElementById('dd-d').value/100,T:+document.getElementById('dd-T').value,P:+document.getElementById('dd-P').value,dep:+document.getElementById('dd-dep').value/100};}
    function dpoV(){return{F:+document.getElementById('dpo-F').value,T:+document.getElementById('dpo-T').value,T2:+document.getElementById('dpo-T2').value,bapr:+document.getElementById('dpo-apr').value/100};}
    function ddCalc(v){var dE=Math.max(v.T-v.P,0);var apr=dE>0?(v.d/(1-v.d))*(360/dE):0;var co=v.F*(1-v.d);var gain=v.F*v.d;var opp=co*v.dep*dE/360;var net=gain-opp;var cy=v.T>0?360/v.T:0;return{dE,apr,co,gain,opp,net,cy,ann:net*cy};}
    function dpoCalc(v){var ext=Math.max(v.T2-v.T,0);var save=v.F*v.bapr*ext/360;var cy=v.T>0?360/v.T:0;return{ext,save,cy,ann:save*cy};}

    /* number wrapping helper - keeps digits LTR inside RTL text */
    function mn(s){return '<span class="mono-num">'+s+'</span>';}

    /* calculator translation helper - loads strings from LanguageService i18n files */
    function ct(key, params){
      var tr = __self.lang.t('calculator.' + key);
      if(params){
        for(var k in params){
          if(params.hasOwnProperty(k)){
            tr = tr.replace(new RegExp('{{'+k+'}}','g'), params[k]);
          }
        }
      }
      return tr;
    }

    /* ============== STEP CONTENT - DYNAMIC DISCOUNTING ============== */
    var ddSteps=[
      {t:ct('steps.dd.step1.title'), ti:ct('steps.dd.step1.subtitle'), fn:function(v,r){
        return ct('steps.dd.step1.body', {amount: mn('JOD '+N(v.F)), days: mn(v.T), discount: mn(P1(v.d)), payDay: mn(v.P), depositRate: mn(P1(v.dep))}) +
        '<div class="step-form">'+ct('steps.dd.step1.inputs', {amount: mn('JOD '+N(v.F)), discount: mn(P1(v.d)), payDay: mn(v.P), dueDays: mn(v.T), daysEarly: mn(r.dE)})+'</div>';
      }},
      {t:ct('steps.dd.step2.title'), ti:ct('steps.dd.step2.subtitle'), fn:function(v,r){
        return ct('steps.dd.step2.body', {amount: mn('JOD '+N(r.co)), gain: mn('JOD '+N(r.gain))}) +
        '<div class="step-form">'+ct('steps.dd.step2.formula', {invoice: mn(N(v.F)), discount: mn(P1(v.d)), amount: mn('JOD '+N(r.co)), gain: mn('JOD '+N(r.gain))})+'</div>';
      }},
      {t:ct('steps.dd.step3.title'), ti:ct('steps.dd.step3.subtitle'), fn:function(v,r){
        return ct('steps.dd.step3.body', {amount: mn('JOD '+N(r.co)), daysEarly: mn(r.dE)}) +
        '<div class="step-form">'+ct('steps.dd.step3.formula', {amount: mn('JOD '+N(r.co)), depositRate: mn(P1(v.dep)), daysEarly: mn(r.dE), cost: mn('JOD '+N2(r.opp))})+'</div>';
      }},
      {t:ct('steps.dd.step4.title'), ti:ct('steps.dd.step4.subtitle'), fn:function(v,r){
        var ok=r.net>=0;
        return '<div class="step-form">'+ct('steps.dd.step4.formula', {gain: mn('JOD '+N(r.gain)), cost: mn('JOD '+N2(r.opp)), net: mn('JOD '+N2(r.net)), terms: mn(v.T), ann: mn('JOD '+N2(r.ann)+' / yr')})+'</div>'+
        '<div class="verdict-pill '+(ok?'ok':'no')+'">'+(ok?ct('steps.dd.step4.verdictWin'):ct('steps.dd.step4.verdictLose'))+'</div>';
      }},
      {t:ct('steps.dd.step5.title'), ti:ct('steps.dd.step5.subtitle'), fn:function(v,r){
        var win=r.apr>=v.dep;
        return '<p class="step-body">'+ct('steps.dd.step5.body', {discount: mn(P1(v.d)), daysEarly: mn(r.dE)})+'</p>'+
        '<div class="step-form">'+ct('steps.dd.step5.formula', {d: mn(v.d.toFixed(3)), oneMinusD: mn((1-v.d).toFixed(3)), daysEarly: mn(r.dE), apr: mn(P1(r.apr)), depositRate: mn(P1(v.dep)), pp: mn(((r.apr-v.dep)*100).toFixed(1)), favour: win?ct('steps.dd.step5.inYourFavour'):ct('steps.dd.step5.inDepositFavour')})+'</div>';
      }}
    ];

    /* ============== STEP CONTENT - EXTEND DPO ============== */
    var dpoSteps=[
      {t:ct('steps.dpo.step1.title'), ti:ct('steps.dpo.step1.subtitle'), fn:function(v,r){
        return ct('steps.dpo.step1.body', {amount: mn('JOD '+N(v.F)), days: mn(v.T), repayDay: mn(v.T2), extraDays: mn(r.ext)}) +
        '<div class="step-form">'+ct('steps.dpo.step1.timeline', {days: mn(v.T), repayDay: mn(v.T2), extraDays: mn(r.ext), apr: mn(P2(v.bapr))})+'</div>'+
        '<div class="verdict-pill ok">'+ct('steps.dpoStep1Verdict', {save: mn('JOD '+N2(r.save))})+'</div>';
      }},
      {t:ct('steps.dpo.step2.title'), ti:ct('steps.dpo.step2.subtitle'), fn:function(v,r){
        return ct('steps.dpo.step2.body', {extraDays: mn(r.ext), apr: mn(P2(v.bapr))}) +
        '<div class="step-form">'+ct('steps.dpo.step2.formula', {invoice: mn(N(v.F)), apr: mn(P2(v.bapr)), extraDays: mn(r.ext), save: mn('JOD '+N2(r.save))})+'</div>';
      }},
      {t:ct('steps.dpo.step3.title'), ti:ct('steps.dpo.step3.subtitle'), fn:function(v,r){
        return ct('steps.dpo.step3.body', {cycles: mn(r.cy.toFixed(1)), days: mn(v.T)}) +
        '<div class="step-form">'+ct('steps.dpo.step3.formula', {save: mn('JOD '+N2(r.save)), cycles: mn(r.cy.toFixed(1)), ann: mn('JOD '+N2(r.ann)+' / yr')})+'</div>';
      }},
      {t:ct('steps.dpo.step4.title'), ti:ct('steps.dpo.step4.subtitle'), fn:function(v,r){
        return ct('steps.dpo.step4.body') +
        '<div class="step-form">'+ct('steps.dpo.step4.partners')+'</div>';
      }},
      {t:ct('steps.dpo.step5.title'), ti:ct('steps.dpo.step5.subtitle'), fn:function(v,r){
        return '<div class="step-form">'+
          '<span class="hi">'+ct('steps.dpo.step5.option1')+'</span>\n'+ct('steps.dpo.step5.option1Desc')+'\n\n'+
          '<span class="hi">'+ct('steps.dpo.step5.option2')+'</span>\n'+ct('steps.dpo.step5.option2Desc')+'\n\n'+
          ct('steps.dpo.step5.summary', {save: mn('JOD '+N2(r.save)), ann: mn('JOD '+N2(r.ann)+' / yr')})+
        '</div>'+
        '<div class="verdict-pill ok">'+ct('steps.bothOptions')+'</div>';
      }}
    ];

    /* ============== UI BUILDERS ============== */
    function bPills(m,n,cur){
      var el=document.getElementById(m+'-pills');el.innerHTML='';
      for(var i=0;i<n;i++){var b=document.createElement('button');b.className='sp'+(i===cur?' on':'');b.textContent=i+1;(function(ii){b.onclick=function(){m==='dd'?(dds=ii,ddR()):(dpos=ii,dpoR());};})(i);el.appendChild(b);}
    }
    function bNav(m,cur,tot){
      var el=document.getElementById(m+'-sn');el.innerHTML='';
      var backTxt = ct('steps.back');
      var nextTxt = ct('steps.next');
      var contactTxt = ct('steps.contact');
      if(cur>0){var bb=document.createElement('button');bb.className='back-btn';bb.textContent=backTxt;bb.onclick=function(){m==='dd'?(dds--,ddR()):(dpos--,dpoR());};el.appendChild(bb);}
      if(cur<tot-1){var nb=document.createElement('button');nb.className='next-btn';nb.innerHTML=nextTxt+' <svg width="16" height="16" viewBox="0 0 16 16" fill="none"><path d="M3 8h10M9 4l4 4-4 4" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>';nb.onclick=function(){m==='dd'?(dds++,ddR()):(dpos++,dpoR());};el.appendChild(nb);}
      else{var ab=document.createElement('a');ab.className='ask-btn';ab.href='mailto:support@credit-plus.me?subject=Credit%20Plus%20Question';ab.innerHTML=contactTxt+' <svg width="14" height="14" viewBox="0 0 14 14" fill="none"><path d="M3 7h8M8 4l3 3-3 3" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/></svg>';el.appendChild(ab);}
    }
    function ddR(){
      var v=ddV(),r=ddCalc(v),steps=ddSteps;
      document.getElementById('ddv-F').textContent='JOD '+N(v.F);
      document.getElementById('ddv-d').textContent=P1(v.d);
      document.getElementById('ddv-T').textContent=v.T+'d';
      document.getElementById('ddv-P').textContent=(curLang==='ar'?ct('steps.day')+' '+mn(v.P):ct('steps.day')+' '+v.P);
      document.getElementById('ddv-dep').textContent=P1(v.dep);
      document.getElementById('dd-st').textContent=steps[dds].t;
      document.getElementById('dd-sc').innerHTML='<div class="step-title">'+steps[dds].ti+'</div>'+steps[dds].fn(v,r);
      bPills('dd',steps.length,dds);bNav('dd',dds,steps.length);
      document.getElementById('dd-big').textContent='JOD '+N2(r.net);
      var perCycle = ct('steps.perCycle');
      var perYear = ct('steps.perYear');
      document.getElementById('dd-vsub').innerHTML=perCycle+'   '+mn('~JOD '+N2(r.ann))+perYear;
      document.getElementById('dd-apr').textContent=P1(r.apr);
      document.getElementById('dd-dep2').textContent=P1(v.dep);
      var w=r.apr>=v.dep;
      document.getElementById('dd-ab1').className='apr-box'+(w?' win':'');
      document.getElementById('dd-ab2').className='apr-box'+(!w?' win':'');
      document.getElementById('dd-c1').style.display=w?'block':'none';
      document.getElementById('dd-c2').style.display=w?'none':'block';

      var rows =
        '<div class="t-row"><span class="t-k">'+ct('steps.invoiceAmount')+'</span><span class="t-v">'+mn('JOD '+N(v.F))+'</span></div>'+
        '<div class="t-row"><span class="t-k">'+ct('steps.daysPaidEarly')+'</span><span class="t-v">'+mn(r.dE)+' '+ct('steps.days')+'</span></div>'+
        '<div class="t-row"><span class="t-k">'+ct('steps.cashYouPay')+'</span><span class="t-v">'+mn('JOD '+N(r.co))+'</span></div>'+
        '<div class="t-row"><span class="t-k">'+ct('steps.discountYouEarn')+'</span><span class="t-v pos">'+mn('+ JOD '+N(r.gain))+'</span></div>'+
        '<div class="t-row"><span class="t-k">'+ct('steps.depositInterestGivenUp')+'</span><span class="t-v neg">'+mn('- JOD '+N2(r.opp))+'</span></div>'+
        '<div class="t-row t-tot"><span class="t-k">'+ct('steps.netBenefitThisCycle')+'</span><span class="t-v pos">'+mn('JOD '+N2(r.net))+'</span></div>'+
        '<div class="t-row t-tot" style="border-top:none;padding-top:4px"><span class="t-k">'+ct('steps.perYearCycles', {cycles: mn(r.cy.toFixed(1))})+'</span><span class="t-v pos">'+mn('JOD '+N2(r.ann)+' / yr')+'</span></div>';
      document.getElementById('dd-rows').innerHTML=rows;

      document.getElementById('dd-note').innerHTML = w
        ? ct('steps.ddWinNote', {discount: mn(P1(v.d)), daysEarly: mn(r.dE), apr: mn(P1(r.apr)), pp: mn(((r.apr-v.dep)*100).toFixed(1)), depositRate: mn(P1(v.dep))})
        : ct('steps.ddLoseNote');
    }
    function dpoR(){
      var v=dpoV(),r=dpoCalc(v),steps=dpoSteps;
      document.getElementById('dpov-F').textContent='JOD '+N(v.F);
      document.getElementById('dpov-T').textContent=v.T+'d';
      document.getElementById('dpov-T2').textContent=v.T2+'d';
      document.getElementById('dpov-apr').textContent=P2(v.bapr);
      document.getElementById('dpo-st').textContent=steps[dpos].t;
      document.getElementById('dpo-sc').innerHTML='<div class="step-title">'+steps[dpos].ti+'</div>'+steps[dpos].fn(v,r);
      bPills('dpo',steps.length,dpos);bNav('dpo',dpos,steps.length);
      document.getElementById('dpo-big').textContent='JOD '+N2(r.save);
      var perCycle = ct('steps.perCycle');
      var perYear = ct('steps.perYear');
      document.getElementById('dpo-vsub').innerHTML=perCycle+'   '+mn('~JOD '+N2(r.ann))+perYear;

      var rows =
        '<div class="t-row"><span class="t-k">'+ct('steps.invoiceAmount')+'</span><span class="t-v">'+mn('JOD '+N(v.F))+'</span></div>'+
        '<div class="t-row"><span class="t-k">'+ct('steps.termsExtendedBy')+'</span><span class="t-v">'+mn(r.ext)+' '+ct('steps.days')+'</span></div>'+
        '<div class="t-row"><span class="t-k">'+ct('steps.cashYouKeepLonger')+'</span><span class="t-v pos">'+mn('JOD '+N(v.F))+'</span></div>'+
        '<div class="t-row"><span class="t-k">'+ct('steps.borrowingAprAvoided')+'</span><span class="t-v">'+mn(P2(v.bapr))+'</span></div>'+
        '<div class="t-row t-tot"><span class="t-k">'+ct('steps.borrowingCostAvoided')+'</span><span class="t-v pos">'+mn('JOD '+N2(r.save))+'</span></div>'+
        '<div class="t-row t-tot" style="border-top:none;padding-top:4px"><span class="t-k">'+ct('steps.perYearCycles', {cycles: mn(r.cy.toFixed(1))})+'</span><span class="t-v pos">'+mn('JOD '+N2(r.ann)+' / yr')+'</span></div>';
      document.getElementById('dpo-rows').innerHTML=rows;

      document.getElementById('dpo-note').innerHTML = ct('steps.dpoNote', {payDay: mn(v.T), repayDay: mn(v.T2), amount: mn(N(v.F)), extraDays: mn(r.ext), apr: mn(P2(v.bapr)), save: mn(N2(r.save))});
    }
    function ddC(){ddR();}
    function dpoC(){dpoR();}

    function selMode(m){
      document.getElementById('mode-dd').classList.toggle('on',m==='dd');
      document.getElementById('mode-dpo').classList.toggle('on',m==='dpo');
      document.getElementById('opt-dd').className='opt'+(m==='dd'?' a-dd':'');
      document.getElementById('opt-dpo').className='opt'+(m==='dpo'?' a-dpo':'');
      document.getElementById('calc-anchor').scrollIntoView({behavior:'smooth',block:'start'});
      m==='dd'?ddR():dpoR();
    }

    /* ============== LANGUAGE SWITCHER ============== */
    function applyTranslations(lang){
      document.querySelectorAll('[data-i18n]').forEach(function(el){
        var key = el.getAttribute('data-i18n');
        var tr = __self.lang.t(key);
        if(tr !== key){ el.innerHTML = tr; }
      });
    }
    function setLang(lang){
      var calcLang = lang;
      curLang = lang;
      document.getElementById('cpCalcRoot').setAttribute('data-lang', calcLang);
      document.getElementById('cpCalcRoot').setAttribute('dir', lang==='ar'?'rtl':'ltr');
      document.getElementById('cpCalcRoot').setAttribute('lang', calcLang);
      var btnEn = document.getElementById('btn-en');
      var btnAr = document.getElementById('btn-ar');
      if (btnEn) btnEn.classList.toggle('on', lang==='en');
      if (btnAr) btnAr.classList.toggle('on', lang==='ar');
      applyTranslations(calcLang);
      dds=0; dpos=0;
      ddR(); dpoR();
    }

    /* ============== LIVE COUNTER TICKER ============== */
    var BASE=1000000,DR=0.04,DDAPR=0.092,t0=Date.now();
    function tick(){
      var e=(Date.now()-t0)/1000,ds=BASE*DR/31536000,dd=BASE*DDAPR/31536000;
      document.getElementById('cnt-dep').textContent=(ds*e).toFixed(6);
      document.getElementById('cnt-dd').textContent=(dd*e).toFixed(6);
      document.getElementById('cnt-gap').textContent=((dd-ds)*e).toFixed(6);
      if(__alive)requestAnimationFrame(tick);
    }
    __self._setLang = setLang;
    __self._selMode = selMode;
    __self._ddC = ddC;
    __self._dpoC = dpoC;

    setLang(this.lang.currentLang());
    requestAnimationFrame(tick);
  }
}
