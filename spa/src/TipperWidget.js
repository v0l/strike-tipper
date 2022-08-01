import {useRef, useEffect, useState} from "react";
import {useSelector, useDispatch} from "react-redux";
import {setInvoice, addPaymentHistory} from "./WidgetState";
import {Events} from "./Events";

import "./TipperWidget.css";
import Coins from "./coins.mp3";
import {TimeAgo} from "./TimeAgo";
import TipperQR from "./TipperQR";

export function TipperWidget(props) {
    const dispatch = useDispatch();

    let invoice = useSelector((state) => state.widget.invoice);
    let paymentHistory = useSelector((state) => state.widget.paymentHistory);

    const [qrLink, setQrLink] = useState();
    let [loadingInvoice, setLoadingInvoice] = useState(false);
    const fxRef = useRef();

    async function loadInvoice(id) {
        setLoadingInvoice(true);
        try {
            let req = await fetch(`/invoice/${id}`);
            if (req.ok) {
                let inv = await req.json();
                dispatch(setInvoice(inv));
            } else if (req.status === 404) {
                await getNewInvoice();
            }
        } catch (e) {
            console.error(e);
        }
    }

    async function getNewInvoice() {
        setLoadingInvoice(true);
        if(props.useLnurl) {
            await getLNURLConfig();
        } else {
            await getRepeaterInvoice();
        }
    }
    
    async function getLNURLConfig() {
        let req = await fetch(`/lnurlpay/${props.username}?description=${props.description}`, {
            headers: {
                "content-type": "application/json"
            }
        });
        if (req.ok) {
            let inv = await req.json();
            setQrLink(`lightning:${inv.url}`);
            setLoadingInvoice(false);
        }
    }
    
    async function getRepeaterInvoice(){
        let body = {
            handle: props.username,
            description: props.description,
            amount: {
                currency: props.currency,
                amount: props.amount
            }
        };

        let req = await fetch(`/invoice/new`, {
            method: "POST",
            body: JSON.stringify(body),
            headers: {
                "content-type": "application/json"
            }
        });
        if (req.ok) {
            let inv = await req.json();
            if (invoice) {
                Events.RemoveListenId(invoice.invoice.invoiceId);
            }
            dispatch(setInvoice(inv));
        }
    }

    function handleWidgetEvent(ev) {
        if (ev.type === "InvoicePaid") {
            dispatch(addPaymentHistory(ev.data));
            getNewInvoice();

            fxRef.current?.play();
        }
    }

    function renderPayLog(l) {
        let style = {
            style: "currency",
            currency: l.currency === "USDT" ? "USD" : l.currency,
            minimumFractionDigits: 2
        };
        return (
            <div className="tip-msg" key={l.paid}>
                <TimeAgo from={l.paid}/>
                <div><span
                    className="strike-yellow">{l.from ?? "⚡"}</span> tipped {l.amount.toLocaleString(undefined, style)}
                </div>
            </div>
        )
    }

    useEffect(() => {
        if (invoice) {
            let expire = new Date(invoice.quote.expiration);
            let diff = expire.getTime() - new Date().getTime();
            let id = invoice.invoice.invoiceId;

            let t = setTimeout(() => {
                loadInvoice(id);
            }, diff);
            console.log(`Set timout for: ${diff}ms`);

            let link = `lightning:${invoice.quote.lnInvoice}`;
            setQrLink(link);

            setLoadingInvoice(false);
            Events.AddListenId(id)
            return () => clearInterval(t);
        }
    }, [invoice]);

    useEffect(() => {
        getNewInvoice();
        Events.HookEvent("WidgetEvent", function (d) {
            handleWidgetEvent(d.data);
        }.bind(this));
    }, []);

    return (
        <div className="widget">
            <TipperQR link={qrLink} blur={loadingInvoice}/>
            <div className="tip-history">
                {[...(paymentHistory || [])].sort((a, b) => {
                    let ad = new Date(a.paid).getTime();
                    var bd = new Date(b.paid).getTime();
                    return bd - ad;
                }).map(a => renderPayLog(a))}
            </div>
            <audio src={Coins} autoPlay={true} ref={fxRef}/>
        </div>
    );
}