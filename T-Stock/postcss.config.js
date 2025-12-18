module.exports = {
    plugins: [
        require('autoprefixer'),
        require('postcss-pxtorem')({
            rootValue: 16,       // 1rem = 16px
            unitPrecision: 5,    // Decimal precision for rem values
            propList: ['*'],     // Properties to convert, '*' means all
            selectorBlackList: [], // Ignore selectors if needed
            replace: true,       // Replace px with rem
            mediaQuery: false,   // Convert px in media queries
            minPixelValue: 0     // Minimum px value to convert
        })
    ]
};
