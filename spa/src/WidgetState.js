import { createSlice } from '@reduxjs/toolkit'

const ExampleTips = [
    {
        from: "jack",
        paid: new Date(),
        amount: 1000,
        currency: "USD"
    },
    {
        from: "hrf",
        paid: new Date(),
        amount: 10000,
        currency: "EUR"
    },
    {
        from: "kieran",
        paid: new Date(),
        amount: 10.20,
        currency: "GBP"
    },
    {
        paid: new Date(),
        amount: 420.69,
        currency: "USDT"
    }
];

export const widgetSlice = createSlice({
    name: 'widget',
    initialState: {
        paymentHistory: [],
        invoice: null
    },
    reducers: {
        setInvoice: (state, action) => {
            state.invoice = action.payload;
        },
        addPaymentHistory: (state, action) => {
            state.paymentHistory = [
                ...state.paymentHistory,
                action.payload
            ].slice(-5);
        }
    },
})

// Action creators are generated for each case reducer function
export const { setInvoice, addPaymentHistory } = widgetSlice.actions

export default widgetSlice.reducer