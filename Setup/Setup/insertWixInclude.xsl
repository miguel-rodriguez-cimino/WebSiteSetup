<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
                
  <xsl:param name="Configuration"/>

  <xsl:template match="/">
    <xsl:text disable-output-escaping="yes">&#xA;&lt;?include $(sys.CURRENTDIR)\CustomVariables.wxi?&gt;&#xA;</xsl:text>
    <xsl:apply-templates select="@* | node()" />
  </xsl:template>

  <xsl:template match="@* | node()">
    <xsl:copy>
      <xsl:apply-templates select="@* | node()" />
    </xsl:copy>
  </xsl:template>

</xsl:stylesheet>
