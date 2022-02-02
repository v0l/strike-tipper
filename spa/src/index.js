import React from 'react';
import ReactDOM from 'react-dom';
import { Provider } from 'react-redux';
import { RepeaterApp } from "./RepeaterApp";
import store from "./Store";

import './index.css';

ReactDOM.render(
    <React.StrictMode>
        <Provider store={store}>
            <RepeaterApp />
        </Provider>
    </React.StrictMode>,
    document.getElementById('root')
);