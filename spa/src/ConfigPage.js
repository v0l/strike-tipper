import './App.css';
import {useState} from "react";

export function ConfigPage() {
    const [username, setUsername] = useState();
    const [amount, setAmount] = useState(1);
    const [description, setDescription] = useState("Stream tip");
    const [currency, setCurrency] = useState("USD");
    const [config, setConfig] = useState();
    const [useLnurl, setUseLnurl] = useState(false);

    async function handleSubmit(e) {
        setConfig({
            username, currency, amount, description, useLnurl
        });
    }

    function renderConfig() {
        if (config) {
            let url = `/#${btoa(JSON.stringify(config))}`;
            return (
                <div>
                    <br/>
                    <h3>Add the following link as a 'Browser Source' in OBS:</h3>
                    <a href={url} target="_blank" className="widget-link">COPY ME</a>
                    <br/>
                    <h3>Help</h3>
                    <p>
                        The tip widget uses a resolution of 600x700, you should set this as the resolution of the
                        browser in OBS.
                    </p>
                    <p>
                        The tip widget will work in any alternatives to OBS as long as you can use a web page as a
                        'Layer'.
                    </p>
                    <p>
                        The amount is fixed and will allow people to tip ONLY the amount you specify above.
                    </p>
                    <p>
                        Works with any lightning wallet supporting BOLT11 invoices. ⚡
                    </p>
                    <p>
                        If the widget doesn't load, please check the username is correct.
                    </p>
                </div>
            );
        }
        return null;
    }

    return (
        <div className="app">
            <h1>Hello</h1>
            <dl>
                <dt>Strike username:</dt>
                <dd><input type="text" placeholder="Username" onInput={(e) => setUsername(e.target.value)}
                           value={username}/></dd>

                <dt>Use LNURL:</dt>
                <dd><input type="checkbox" onInput={(e) => setUseLnurl(!e.target.checked)} checked={useLnurl}/></dd>
                
                {useLnurl ?
                    null :
                    <>
                        <dt>Tip amount:</dt>
                        <dd><input type="number" placeholder="Amount" onInput={(e) => setAmount(e.target.value)}
                                   value={amount}/></dd>

                        <dt>Currency:</dt>
                        <dd>
                            <select onChange={(e) => setCurrency(e.target.value)}>
                                <option>USD</option>
                                <option>USDT</option>
                                <option>EUR</option>
                            </select>
                        </dd>
                    </>
                }

                <dt>Description:</dt>
                <dd><input type="text" placeholder="Description" onInput={(e) => setDescription(e.target.value)}
                           value={description}/></dd>
            </dl>
            <div className="btn" onClick={handleSubmit}>Submit</div>
            {renderConfig()}
        </div>
    );
}
